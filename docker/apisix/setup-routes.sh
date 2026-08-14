#!/bin/sh
# APISIX route'larını Admin API üzerinden idempotent şekilde oluşturur/günceller.
# docker-compose'daki apisix-routes servisi tarafından her `docker compose up`da
# otomatik çalıştırılır. Elle tekrar çalıştırmak için:
#   docker compose run --rm apisix-routes
#
# Servisler artık dışarıya port açmıyor (bkz. docker-compose.yml) — tüm trafik
# tek giriş noktasından (APISIX :9080) geçiyor. Path'ler servisler arasında
# çakışmadığı için (identity: /api/auth, /api/user — news: /api/article,
# /api/category, /api/tag) rewrite'a gerek yok, APISIX sadece doğru upstream'e
# yönlendiriyor.

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

put_route() {
  id="$1"
  base="$2"           # ör. /api/article — tam bu path (GET listesi vb.) İÇİN
  upstream_host="$3"

  # "/foo/*" deseni yalnızca "/foo/" ile başlayan path'leri eşliyor, "/foo"
  # (sondaki / olmadan gelen liste isteklerini) eşlemiyor — bu yüzden hem tam
  # path'i hem wildcard'ı "uris" dizisiyle birlikte veriyoruz.
  http_code=$(curl -s -o /dev/null -w '%{http_code}' -X PUT "$ADMIN_URL/apisix/admin/routes/$id" \
    -H "X-API-KEY: $ADMIN_KEY" -H 'Content-Type: application/json' \
    -d "{\"uris\":[\"$base\",\"$base/*\"],\"upstream\":{\"type\":\"roundrobin\",\"nodes\":{\"$upstream_host\":1}}}")

  echo "route $id  $base (+ /*) -> $upstream_host  (HTTP $http_code)"
}

put_route 1 "/api/auth"     "identity-service:8080"
put_route 2 "/api/user"     "identity-service:8080"
put_route 3 "/api/article"  "news-service:8080"
put_route 4 "/api/category" "news-service:8080"
put_route 5 "/api/tag"      "news-service:8080"
put_route 6 "/api/media"    "news-service:8080"

echo "APISIX route'ları hazır."
