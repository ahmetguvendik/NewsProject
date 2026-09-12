#!/usr/bin/env bash
#
# Elasticsearch hesaplarını ve Kibana'nın APM entegrasyonunu kurar.
#
# Neden bir betik: bunların hiçbiri compose ile yapılamıyor. Parolalar ve roller
# Elasticsearch'ün kendi API'sinden, entegrasyon ise Kibana'nın Fleet API'sinden
# geçiyor — yani küme ayağa kalkmadan yapılamayacak işler.
#
# Tekrar çalıştırılabilir: her adım "zaten varsa üzerine yaz" biçiminde. Kurulum
# bozulduğunda yeniden çalıştırmak güvenli.
#
# Kullanım:  ./docker/setup-elastic-security.sh
# Ön koşul:  .env dosyasında ELASTIC_PASSWORD, KIBANA_PASSWORD,
#            LOGSTASH_PASSWORD, LOG_READER_PASSWORD tanımlı olmalı.

set -euo pipefail

cd "$(dirname "$0")/.."

if [ ! -f .env ]; then
  echo "HATA: .env bulunamadı." >&2
  exit 1
fi

# .env `source` EDİLMİYOR: içindeki bazı değerler boşluk içeriyor (örneğin
# Gmail uygulama parolası "xxxx yyyy zzzz" biçiminde) ve source bunu komut
# olarak çalıştırmaya kalkıyor. Docker compose dosyayı kendi ayrıştırıcısıyla
# okuduğu için sorun çıkmıyor, kabuk çıkarıyor.
#
# Burada yalnızca ihtiyaç duyulan anahtarlar, satırın ilk `=`'inden sonrası
# olduğu gibi alınarak okunuyor.
read_env() {
  local value
  value=$(grep -m1 "^$1=" .env | cut -d= -f2-)
  if [ -z "$value" ]; then
    echo "HATA: .env dosyasında $1 tanımlı değil." >&2
    exit 1
  fi
  printf '%s' "$value"
}

ELASTIC_PASSWORD=$(read_env ELASTIC_PASSWORD)
KIBANA_PASSWORD=$(read_env KIBANA_PASSWORD)
LOGSTASH_PASSWORD=$(read_env LOGSTASH_PASSWORD)
LOG_READER_PASSWORD=$(read_env LOG_READER_PASSWORD)

ES="http://localhost:9200"
KIBANA="http://localhost:5601"
AUTH="elastic:${ELASTIC_PASSWORD}"

# ── Elasticsearch'ün hazır olmasını bekle ────────────────────────────────────
# Güvenlik açıkken küme, parola veritabanını da kurmak zorunda; bu ilk açılışta
# birkaç saniye sürüyor ve o sırada gelen istek bağlantı hatası alıyor.
echo "Elasticsearch bekleniyor..."
for i in $(seq 1 60); do
  if curl -sf -u "$AUTH" "$ES/_cluster/health" >/dev/null 2>&1; then
    echo "  hazır"
    break
  fi
  [ "$i" -eq 60 ] && { echo "HATA: Elasticsearch 120 sn içinde yanıt vermedi." >&2; exit 1; }
  sleep 2
done

# ── kibana_system parolası ───────────────────────────────────────────────────
# Yerleşik bir kullanıcı, oluşturulmuyor — yalnızca parolası belirleniyor.
# Kibana bu kimlikle bağlanıyor; `elastic` superuser'ını kullanmıyor.
echo "kibana_system parolası ayarlanıyor..."
curl -sf -u "$AUTH" -X POST "$ES/_security/user/kibana_system/_password" \
  -H 'Content-Type: application/json' \
  -d "{\"password\":\"${KIBANA_PASSWORD}\"}" >/dev/null
echo "  tamam"

# ── logstash_writer: yalnızca log yazabilir ──────────────────────────────────
# auto_configure, Logstash'in index'i ilk kez oluşturabilmesi için gerekli
# (index adı tarihe göre değişiyor: logs-{servis}-{tarih}).
echo "logstash_writer rolü ve kullanıcısı..."
curl -sf -u "$AUTH" -X PUT "$ES/_security/role/logstash_writer" \
  -H 'Content-Type: application/json' -d '{
    "cluster": ["monitor", "manage_index_templates"],
    "indices": [{
      "names": ["logs-*"],
      "privileges": ["create_index", "create_doc", "write", "auto_configure"]
    }]
  }' >/dev/null

curl -sf -u "$AUTH" -X PUT "$ES/_security/user/logstash_writer" \
  -H 'Content-Type: application/json' \
  -d "{\"password\":\"${LOGSTASH_PASSWORD}\",\"roles\":[\"logstash_writer\"]}" >/dev/null
echo "  tamam"

# ── log_reader: Grafana panosu ve hata oranı kontrolü ────────────────────────
# Salt okunur. İkisi de yalnızca sorguluyor; yazma yetkisi vermek için sebep yok.
# traces-apm* de dahil: pano ileride trace verisini de gösterebilsin.
echo "log_reader rolü ve kullanıcısı..."
curl -sf -u "$AUTH" -X PUT "$ES/_security/role/log_reader" \
  -H 'Content-Type: application/json' -d '{
    "cluster": ["monitor"],
    "indices": [{
      "names": ["logs-*", "traces-apm*", "metrics-apm*"],
      "privileges": ["read", "view_index_metadata"]
    }]
  }' >/dev/null

curl -sf -u "$AUTH" -X PUT "$ES/_security/user/log_reader" \
  -H 'Content-Type: application/json' \
  -d "{\"password\":\"${LOG_READER_PASSWORD}\",\"roles\":[\"log_reader\"]}" >/dev/null
echo "  tamam"

# ── Kibana'nın hazır olmasını bekle ──────────────────────────────────────────
# kibana_system parolası yukarıda yeni atandı; Kibana o ana kadar 401 alıp
# yeniden deniyor olabilir. "available" durumuna geçmesi bunu da kapsıyor.
echo "Kibana bekleniyor (ilk açılışta 1-2 dakika sürebilir)..."
for i in $(seq 1 90); do
  if curl -sf -u "$AUTH" "$KIBANA/api/status" 2>/dev/null \
     | grep -q '"level":"available"'; then
    echo "  hazır"
    break
  fi
  [ "$i" -eq 90 ] && { echo "HATA: Kibana 180 sn içinde hazır olmadı." >&2; exit 1; }
  sleep 2
done

# ── Fleet ve APM entegrasyonu ────────────────────────────────────────────────
# Asıl mesele bu. APM Server veriyi yazabiliyor ama index ŞABLONLARI olmadan
# Elasticsearch alanları dinamik eşliyor: transaction.name `keyword` yerine
# `text` oluyor ve APM arayüzü servis listesini boş döndürüyor — gruplama
# yapılamadığı için. Şablonları kuran şey bu entegrasyon paketi.
#
# kbn-xsrf başlığı Kibana'nın tüm yazma uçlarında zorunlu; değeri önemsiz,
# varlığı CSRF koruması için kontrol ediliyor.
echo "Fleet kuruluyor..."
curl -sf -u "$AUTH" -X POST "$KIBANA/api/fleet/setup" \
  -H 'kbn-xsrf: true' >/dev/null
echo "  tamam"

echo "APM entegrasyonu kuruluyor..."
RESPONSE=$(curl -s -u "$AUTH" -X POST "$KIBANA/api/fleet/epm/packages/apm" \
  -H 'kbn-xsrf: true' -H 'Content-Type: application/json' \
  -d '{"force":true}')

if echo "$RESPONSE" | grep -q '"items"'; then
  echo "  tamam"
elif echo "$RESPONSE" | grep -q 'already installed'; then
  echo "  zaten kurulu"
else
  echo "HATA: APM entegrasyonu kurulamadı:" >&2
  echo "$RESPONSE" >&2
  exit 1
fi

# ── Doğrulama ────────────────────────────────────────────────────────────────
# Şablonun varlığı kurulumun gerçekten işe yaradığının tek kanıtı; API'nin
# "tamam" demesi yeterli değil.
echo
echo "Doğrulanıyor..."
if curl -sf -u "$AUTH" "$ES/_index_template/traces-apm" >/dev/null 2>&1; then
  echo "  traces-apm index şablonu kuruldu ✓"
else
  echo "  UYARI: traces-apm şablonu bulunamadı — APM arayüzü boş kalabilir." >&2
fi

echo
echo "Kurulum tamamlandı."
echo "  Kibana:  $KIBANA  (kullanıcı: elastic)"
echo "  Parola:  .env → ELASTIC_PASSWORD"
