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

# Tam path'e uygulanan sıkı limit. priority, genel route'un önüne geçmesi için.
put_exact_route() {
  id="$1"; uri="$2"; upstream_host="$3"; count="$4"; window="$5"; message="$6"; description="$7"

  cat > /tmp/route.json <<JSON
{
  "uri": "$uri",
  "priority": 20,
  "upstream": {"type":"roundrobin","nodes":{"$upstream_host":1}},
  $(limit_plugin "$count" "$window" "$message" "$description")
}
JSON

  http_code=$(curl -s -o /dev/null -w '%{http_code}' -X PUT "$ADMIN_URL/apisix/admin/routes/$id" \
    -H "X-API-KEY: $ADMIN_KEY" -H 'Content-Type: application/json' --data @/tmp/route.json)

  echo "route $id  $uri -> $upstream_host  [${count}/${window}sn]  (HTTP $http_code)"
}

put_route() {
  id="$1"; base="$2"; upstream_host="$3"; count="$4"; window="$5"

  # "/foo/*" deseni yalnızca "/foo/" ile başlayanları eşliyor, "/foo"yu (liste
  # isteklerini) eşlemiyor — bu yüzden hem tam path hem wildcard veriliyor.
  cat > /tmp/route.json <<JSON
{
  "uris": ["$base", "$base/*"],
  "upstream": {"type":"roundrobin","nodes":{"$upstream_host":1}},
  $(limit_plugin "$count" "$window" "Çok fazla istek gönderdiniz." "Lütfen kısa bir süre bekleyip tekrar deneyin.")
}
JSON

  http_code=$(curl -s -o /dev/null -w '%{http_code}' -X PUT "$ADMIN_URL/apisix/admin/routes/$id" \
    -H "X-API-KEY: $ADMIN_KEY" -H 'Content-Type: application/json' --data @/tmp/route.json)

  echo "route $id  $base (+ /*) -> $upstream_host  [${count}/${window}sn]  (HTTP $http_code)"
}

# ─── Sıkı limitli uçlar ────────────────────────────────────────────────
# Kayıt: sahte hesap seline karşı saatte 3. Gerçek bir kullanıcı bir kez kaydolur.
put_exact_route 10 "/api/auth/register" "identity-service:8080" 3 3600 \
  "Çok fazla kayıt denemesi yaptınız." "Lütfen bir saat sonra tekrar deneyin."

# Yükleme izni: depo şişirmeye karşı. Bir haber genelde tek görsel alır.
put_exact_route 11 "/api/media/upload-url" "news-service:8080" 20 60 \
  "Çok fazla dosya yükleme isteği gönderdiniz." "Lütfen bir dakika sonra tekrar deneyin."

# ─── Genel route'lar ───────────────────────────────────────────────────
# Okuma ağırlıklı uçlar geniş, yazma ağırlıklılar daha dar tutuldu.
put_route 1 "/api/auth"     "identity-service:8080"  60 60
put_route 2 "/api/user"     "identity-service:8080" 300 60
put_route 3 "/api/article"  "news-service:8080"     300 60
put_route 4 "/api/category" "news-service:8080"     300 60
put_route 5 "/api/tag"      "news-service:8080"     300 60
put_route 6 "/api/media"    "news-service:8080"      60 60
put_route 7 "/api/weather"  "news-service:8080"     300 60

rm -f /tmp/route.json
echo "APISIX route'ları ve rate limit'leri hazır."
