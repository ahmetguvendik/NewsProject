#!/bin/sh
# APISIX route'larını Admin API üzerinden idempotent şekilde oluşturur/günceller.
# `setup` profilinde olduğu için normal `docker compose up` ile çalışmaz:
#   docker compose --profile setup up apisix-routes
#
# Servisler dışarıya port açmıyor (bkz. docker-compose.yml) — tüm trafik tek
# giriş noktasından (APISIX :9080) geçiyor. Path'ler servisler arasında
# çakışmadığı için (identity: /api/auth, /api/user — news: /api/article,
# /api/category, /api/tag, /api/media) rewrite'a gerek yok.
#
# NOT: Giriş (login) buradan GEÇMEZ. Client token'ı doğrudan Keycloak'tan alır,
# bu yüzden brute-force koruması APISIX'te değil Keycloak'ın kendi realm
# ayarında yapılır (bkz. docker/keycloak/news-portal-realm.json).

set -e

ADMIN_URL="${APISIX_ADMIN_URL:-http://apisix:9180}"
ADMIN_KEY="${APISIX_ADMIN_KEY:-meTQLdxtgXepBojnMmjSwAMXlyuKsECj}"

echo "APISIX admin API'sinin hazır olması bekleniyor..."
attempt=0
until curl -s -o /dev/null -w '%{http_code}' "$ADMIN_URL/apisix/admin/routes" -H "X-API-KEY: $ADMIN_KEY" | grep -qE '^(200|404)$'; do
  attempt=$((attempt + 1))
  if [ "$attempt" -ge 30 ]; then
    echo "APISIX admin API 60 saniye içinde hazır olmadı, vazgeçiliyor." >&2
    exit 1
  fi
  sleep 2
done

# ─── Rate limit ────────────────────────────────────────────────────────
# policy "local": sayaç APISIX'in kendi belleğinde tutulur. Tek düğüm
# çalıştığımız için sonuç birebir doğru ve Redis'e bağımlılık doğurmuyor —
# depo çökünce giriş/kayıt kapanmıyor.
#
# UYARI: birden fazla APISIX düğümüne çıkılırsa her düğüm kendi sayacını tutar
# ve limit sessizce düğüm sayısı kadar katlanır (hata vermez, sadece gevşer).
# O noktada policy "redis" yapılmalı.
#
# limit-count'un rejected_msg'i tek başına yetmiyor: APISIX onu
# {"error_msg": "..."} içine sarmalıyor, bu yüzden client gövdedeki errorCode'u
# okuyamıyor. response-rewrite yalnızca 429 yanıtlarında (vars: status == 429)
# gövdeyi uygulamanın ErrorResponse şekline çeviriyor; böylece client'taki
# ErrorAlert hiç değişmeden anlamlı mesajı gösteriyor. Retry-After da burada
# ekleniyor — limit-count'un kendisi bu başlığı üretmiyor.
limit_plugin() {
  count="$1"; window="$2"; message="$3"; description="$4"
  cat <<JSON
"plugins":{
  "limit-count":{"count":$count,"time_window":$window,"key_type":"var","key":"remote_addr","policy":"local","rejected_code":429,"show_limit_quota_header":true,"rejected_msg":"$message"},
  "response-rewrite":{"vars":[["status","==",429]],"headers":{"set":{"Content-Type":"application/json; charset=utf-8","Retry-After":"$window"}},"body":"{\\"status\\":429,\\"errorCode\\":\\"RATE_LIMIT_EXCEEDED\\",\\"message\\":\\"$message\\",\\"description\\":\\"$description\\",\\"errors\\":null}"}
}
JSON
}

# ─── Upstream'ler ──────────────────────────────────────────────────────
# Hedef servisler route'ların içine gömülü değil, bağımsız nesneler olarak
# tanımlanıyor ve route'lar upstream_id ile referans veriyor.
#
# Sebep: news-service beş ayrı route'ta hedef (article, category, tag, media,
# weather). Gömülü tanımda port değişse ya da sağlık kontrolü eklensin istense
# beş route birden düzenlenmesi gerekirdi. Bağımsız nesnede tek yer.
#
# Ayrıca APISIX Dashboard'ın Upstreams sekmesi yalnızca bağımsız nesneleri
# listeliyor; gömülü tanımlar orada görünmüyordu.
put_upstream() {
  id="$1"; host="$2"

  cat > /tmp/upstream.json <<JSON
{
  "type": "roundrobin",
  "nodes": {"$host": 1}
}
JSON

  http_code=$(curl -s -o /dev/null -w '%{http_code}' -X PUT "$ADMIN_URL/apisix/admin/upstreams/$id" \
    -H "X-API-KEY: $ADMIN_KEY" -H 'Content-Type: application/json' --data @/tmp/upstream.json)

  echo "upstream $id -> $host  (HTTP $http_code)"
}

put_upstream "identity-service"     "identity-service:8080"
put_upstream "news-service"         "news-service:8080"
put_upstream "notification-service" "notification-service:8080"

# ─── Route'lar ─────────────────────────────────────────────────────────
# Tam path'e uygulanan sıkı limit. priority, genel route'un önüne geçmesi için.
put_exact_route() {
  id="$1"; uri="$2"; upstream_id="$3"; count="$4"; window="$5"; message="$6"; description="$7"

  cat > /tmp/route.json <<JSON
{
  "uri": "$uri",
  "priority": 20,
  "upstream_id": "$upstream_id",
  $(limit_plugin "$count" "$window" "$message" "$description")
}
JSON

  http_code=$(curl -s -o /dev/null -w '%{http_code}' -X PUT "$ADMIN_URL/apisix/admin/routes/$id" \
    -H "X-API-KEY: $ADMIN_KEY" -H 'Content-Type: application/json' --data @/tmp/route.json)

  echo "route $id  $uri -> $upstream_id  [${count}/${window}sn]  (HTTP $http_code)"
}

put_route() {
  id="$1"; base="$2"; upstream_id="$3"; count="$4"; window="$5"

  # "/foo/*" deseni yalnızca "/foo/" ile başlayanları eşliyor, "/foo"yu (liste
  # isteklerini) eşlemiyor — bu yüzden hem tam path hem wildcard veriliyor.
  cat > /tmp/route.json <<JSON
{
  "uris": ["$base", "$base/*"],
  "upstream_id": "$upstream_id",
  $(limit_plugin "$count" "$window" "Çok fazla istek gönderdiniz." "Lütfen kısa bir süre bekleyip tekrar deneyin.")
}
JSON

  http_code=$(curl -s -o /dev/null -w '%{http_code}' -X PUT "$ADMIN_URL/apisix/admin/routes/$id" \
    -H "X-API-KEY: $ADMIN_KEY" -H 'Content-Type: application/json' --data @/tmp/route.json)

  echo "route $id  $base (+ /*) -> $upstream_id  [${count}/${window}sn]  (HTTP $http_code)"
}

# ─── Sağlık uçları ─────────────────────────────────────────────────────
# Servisler host'a port açmadığı için sağlık uçları yalnızca konteyner ağından
# erişilebiliyordu. Bu route'lar tarayıcıdan bakabilmek için açıyor.
#
# UYARI: bağımlılıkların adlarını ve durumlarını dışarı veriyorlar. Geliştirme
# ortamı için sorun değil; canlıya çıkarken ya kaldırılmalı ya da kimlik
# doğrulamasıyla korunmalı.
#
# proxy-rewrite şart: dışarıda /health/news, serviste /health.
put_health_route() {
  id="$1"; uri="$2"; upstream_id="$3"; target="$4"

  cat > /tmp/route.json <<JSON
{
  "uri": "$uri",
  "priority": 30,
  "upstream_id": "$upstream_id",
  "plugins": {
    "proxy-rewrite": {"uri": "$target"},
    "limit-count": {"count":120,"time_window":60,"key_type":"var","key":"remote_addr","policy":"local","rejected_code":429}
  }
}
JSON

  http_code=$(curl -s -o /dev/null -w '%{http_code}' -X PUT "$ADMIN_URL/apisix/admin/routes/$id" \
    -H "X-API-KEY: $ADMIN_KEY" -H 'Content-Type: application/json' --data @/tmp/route.json)

  echo "route $id  $uri -> $upstream_id$target  (HTTP $http_code)"
}

put_health_route 30 "/health/news"         "news-service"         "/health"
put_health_route 31 "/health/identity"     "identity-service"     "/health"
put_health_route 32 "/health/notification" "notification-service" "/health"

# ─── Sıkı limitli uçlar ────────────────────────────────────────────────
# Kayıt: sahte hesap seline karşı.
#
# ÖNCEDEN SAATTE 3'TÜ VE KULLANILAMAZ HALDEYDİ. Sayaç isteğin SONUCUNA bakmıyor —
# APISIX sınırı yanıt üretilmeden önce uyguluyor, dolayısıyla doğrulamaya takılan
# denemeler de kotadan düşüyordu. E-postasını üç kez yanlış yazan bir kullanıcı
# bir saat boyunca hiç kaydolamıyordu; yani sınır kötü niyetliyi değil, acemi
# kullanıcıyı cezalandırıyordu.
#
# Pencere kısaltıldı ve kota genişletildi: elini yanlış atan kullanıcının beş
# denemesi var ve takılırsa bir saat değil on dakika bekliyor. Kötüye kullanım
# tarafında kayıp küçük — saatte 3 yerine 30 hesap, ki sahte hesap seline karşı
# asıl caydırıcı zaten e-posta doğrulaması olmalı (henüz yok, ayrı bir iş).
#
# NOT: sayaç remote_addr üzerinden ve APISIX şu an uçta duruyor, yani bu gerçek
# istemci adresi. Öne bir yük dengeleyici konulursa herkes tek kotayı paylaşır;
# o zaman anahtarın X-Forwarded-For'a taşınması ve APISIX'in real_ip_from
# ayarının o proxy'yi kapsaması gerekir (bkz. apisix_conf/config.yaml).
put_exact_route 10 "/api/auth/register" "identity-service" 5 600 \
  "Çok fazla kayıt denemesi yaptınız." "Lütfen on dakika sonra tekrar deneyin."

# Yükleme izni: depo şişirmeye karşı. Bir haber genelde tek görsel alır.
put_exact_route 11 "/api/media/upload-url" "news-service" 20 60 \
  "Çok fazla dosya yükleme isteği gönderdiniz." "Lütfen bir dakika sonra tekrar deneyin."

# ─── Genel route'lar ───────────────────────────────────────────────────
# Okuma ağırlıklı uçlar geniş, yazma ağırlıklılar daha dar tutuldu.
put_route 1 "/api/auth"     "identity-service"  60 60
put_route 2 "/api/user"     "identity-service" 300 60
put_route 3 "/api/article"  "news-service"     300 60
put_route 4 "/api/category" "news-service"     300 60
put_route 5 "/api/tag"      "news-service"     300 60
put_route 6 "/api/media"    "news-service"      60 60
put_route 7 "/api/weather"  "news-service"     300 60

rm -f /tmp/route.json /tmp/upstream.json
echo "APISIX route'ları ve rate limit'leri hazır."
