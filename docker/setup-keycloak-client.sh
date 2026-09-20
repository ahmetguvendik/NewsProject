#!/usr/bin/env bash
#
# news-portal-client'ın güvenlik ayarlarını daraltır.
#
# ÇÖZDÜĞÜ SORUN: istemci geliştirme kolaylığı için sonuna kadar açık bırakılmıştı
# ve bu dörtlü bir arada hesap devralmaya izin veriyor:
#
#   publicClient:               true     (secret yok)
#   standardFlowEnabled:        true     (authorization code akışı açık)
#   redirectUris:               ["*"]    (kod herhangi bir adrese gönderilebilir)
#   pkce.code.challenge.method: yok      (kodu token'a çevirmek için engel yok)
#
# Ölçüldü — Keycloak rastgele bir adresi kabul ediyordu:
#
#   GET /realms/news-portal/protocol/openid-connect/auth
#         ?client_id=news-portal-client&response_type=code
#         &redirect_uri=https://saldirgan.example/cal
#   → HTTP 200, giriş sayfası
#
# Keycloak oturumu açık bir kullanıcı böyle bir bağlantıya tıklasa kod sessizce
# üretilip saldırganın sunucusuna giderdi; istemci public ve PKCE zorunlu
# olmadığı için token'a çevirmek önünde engel kalmazdı.
#
# Uygulama bu akışı HİÇ kullanmıyor — giriş direct access grant ile yapılıyor
# (bkz. src/Client/src/api/auth.ts). Yani kapı sadece açık duruyordu.
#
# NEDEN standardFlow KAPATILMIYOR: "şifremi unuttum" akışı Keycloak'ın kendi
# sayfalarını kullanıyor (login-actions/reset-credentials). Onu kırma riskine
# girmek yerine iki dar önlem alınıyor; ikisi de saldırıyı tek başına keser:
#   1. redirect_uri artık yalnızca bilinen origin'ler
#   2. PKCE S256 zorunlu — kod çalınsa bile verifier olmadan token'a çevrilemez
#
# NEDEN AYRI BİR BETİK: realm import YALNIZCA ilk açılışta uygulanıyor. Ayakta
# olan bir kurulumda realm JSON'ını değiştirmek hiçbir şey yapmıyor. JSON sıfırdan
# kurulumu, bu betik mevcut kurulumu hizaya getiriyor. (Aynı gerekçe
# setup-keycloak-mail.sh için de geçerli.)
#
# Tekrar çalıştırılabilir.
#
# Kullanım:  ./docker/setup-keycloak-client.sh

set -euo pipefail

cd "$(dirname "$0")/.."

JQ="${JQ:-jq}"
command -v "$JQ" >/dev/null || { echo "HATA: jq bulunamadı." >&2; exit 1; }

# .env `source` EDİLMİYOR: bazı değerler boşluk içeriyor ve source bunu komut
# olarak çalıştırmaya kalkıyor (bkz. setup-keycloak-mail.sh).
read_env_or() {
  local value=""
  [ -f .env ] && value=$(grep -m1 "^$1=" .env | cut -d= -f2- || true)
  printf '%s' "${value:-$2}"
}

# Tarayıcının uygulamayı açtığı adres(ler). Virgülle birden fazla verilebilir;
# üretimde gerçek alan adı eklenmeli.
ORIGINS=$(read_env_or PUBLIC_WEB_ORIGIN "http://localhost:5173")

KC="${KEYCLOAK_URL:-http://localhost:8080}"
REALM="${KEYCLOAK_REALM:-news-portal}"
CLIENT_ID="${KEYCLOAK_CLIENT_ID:-news-portal-client}"
ADMIN_USER="${KEYCLOAK_ADMIN:-admin}"
ADMIN_PASS="${KEYCLOAK_ADMIN_PASSWORD:-admin}"

echo "Keycloak bekleniyor..."
for i in $(seq 1 60); do
  if curl -sf "$KC/realms/$REALM/.well-known/openid-configuration" >/dev/null 2>&1; then
    echo "  hazır"; break
  fi
  [ "$i" -eq 60 ] && { echo "HATA: Keycloak 120 sn içinde yanıt vermedi." >&2; exit 1; }
  sleep 2
done

TOKEN=$(curl -s -X POST "$KC/realms/master/protocol/openid-connect/token" \
  -d "client_id=admin-cli" -d "username=$ADMIN_USER" -d "password=$ADMIN_PASS" \
  -d "grant_type=password" | "$JQ" -r '.access_token // empty')

[ -n "$TOKEN" ] || { echo "HATA: Keycloak yönetici token'ı alınamadı." >&2; exit 1; }

UUID=$(curl -s -H "Authorization: Bearer $TOKEN" \
  "$KC/admin/realms/$REALM/clients?clientId=$CLIENT_ID" | "$JQ" -r '.[0].id // empty')

[ -n "$UUID" ] || { echo "HATA: '$CLIENT_ID' istemcisi bulunamadı." >&2; exit 1; }

echo "Mevcut ayar:"
curl -s -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/$REALM/clients/$UUID" \
  | "$JQ" -r '"  redirectUris: \(.redirectUris) | webOrigins: \(.webOrigins) | pkce: \(.attributes["pkce.code.challenge.method"] // "yok")"'

# Tam nesne okunup üzerine yazılıyor: Keycloak'ın istemci güncelleme ucu kısmi
# gövdede diğer alanları sessizce sıfırlayabiliyor.
#
# redirectUris origin başına "/*" ile genişletiliyor — SPA'nın hangi yolda
# olduğu değişebilir, ama origin sabit. webOrigins'te ise yol OLMAZ, Keycloak
# orada yalnızca şema+host+port bekliyor.
curl -s -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/$REALM/clients/$UUID" \
  | "$JQ" --arg origins "$ORIGINS" '
      ($origins | split(",") | map(sub("^\\s+";"") | sub("\\s+$";"")) | map(select(length > 0))) as $list
      | .webOrigins   = $list
      | .redirectUris = ($list | map(sub("/$";"") + "/*"))
      | .attributes   = ((.attributes // {}) + {"pkce.code.challenge.method": "S256"})
    ' > /tmp/kc-client.json

http_code=$(curl -s -o /dev/null -w '%{http_code}' -X PUT \
  "$KC/admin/realms/$REALM/clients/$UUID" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  --data-binary @/tmp/kc-client.json)

rm -f /tmp/kc-client.json

[ "$http_code" = "204" ] || { echo "HATA: istemci güncellenemedi (HTTP $http_code)." >&2; exit 1; }

echo "Yeni ayar:"
curl -s -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/$REALM/clients/$UUID" \
  | "$JQ" -r '"  redirectUris: \(.redirectUris) | webOrigins: \(.webOrigins) | pkce: \(.attributes["pkce.code.challenge.method"])"'

echo
echo "Tamam. Üretime çıkarken .env içindeki PUBLIC_WEB_ORIGIN'e gerçek alan adını"
echo "ekleyip bu betiği tekrar çalıştırın."
