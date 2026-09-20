#!/usr/bin/env bash
#
# Mevcut kullanıcıları "e-postası doğrulanmış" işaretler.
#
# NEDEN GEREKLİ: realm'de verifyEmail açıldığı anda, e-postası doğrulanmamış HER
# hesap giriş yapamaz hale geliyor — direct grant "invalid_grant / Account is not
# fully set up" dönüyor. Bayrak, doğrulama özelliği yokken açılmış hesapları da
# kapsadığı için kurulumdaki herkes bir anda dışarıda kalıyor.
#
# NEDEN setup-keycloak-mail.sh'İN İÇİNDE DEĞİL: bu bir güvenlik kararı. "Bu
# insanların e-postasına gerçekten sahip olduğunu varsayıyorum" demek, bayrağı
# açmanın yan etkisi olarak sessizce verilecek bir karar değil. Mail betiği
# yalnızca uyarıp bu komutu öneriyor, çalıştırmayı kullanıcıya bırakıyor.
#
# GELİŞTİRME İÇİN. Gerçek bir kurulumda doğru davranış, mevcut kullanıcılara
# doğrulama bağlantısı göndermek (Keycloak yönetim arayüzü → Credentials →
# Credential Reset → Verify Email) ya da onları doğrulanmamış bırakmaktır.
#
# Kullanım:  ./docker/verify-existing-users.sh

set -euo pipefail

cd "$(dirname "$0")/.."

JQ="${JQ:-jq}"
command -v "$JQ" >/dev/null || { echo "HATA: jq bulunamadı." >&2; exit 1; }

KC="${KEYCLOAK_URL:-http://localhost:8080}"
REALM="${KEYCLOAK_REALM:-news-portal}"
ADMIN_USER="${KEYCLOAK_ADMIN:-admin}"
ADMIN_PASS="${KEYCLOAK_ADMIN_PASSWORD:-admin}"

TOKEN=$(curl -s -X POST "$KC/realms/master/protocol/openid-connect/token" \
  -d "client_id=admin-cli" -d "username=$ADMIN_USER" -d "password=$ADMIN_PASS" \
  -d "grant_type=password" | "$JQ" -r '.access_token // empty')

[ -n "$TOKEN" ] || { echo "HATA: Keycloak yönetici token'ı alınamadı." >&2; exit 1; }

PENDING=$(curl -s -H "Authorization: Bearer $TOKEN" "$KC/admin/realms/$REALM/users?max=1000" \
  | "$JQ" -r '[.[] | select(.emailVerified != true)] | .[] | "\(.id)\t\(.email // .username)"')

if [ -z "$PENDING" ]; then
  echo "Doğrulanmamış kullanıcı yok."
  exit 0
fi

printf '%s' '{"emailVerified":true}' > /tmp/verified.json

printf '%s\n' "$PENDING" | while IFS=$'\t' read -r id email; do
  code=$(curl -s -o /dev/null -w '%{http_code}' -X PUT \
    "$KC/admin/realms/$REALM/users/$id" \
    -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
    --data-binary @/tmp/verified.json)
  printf '  %-36s → HTTP %s\n' "$email" "$code"
done

rm -f /tmp/verified.json

echo
echo "Tamam. Yeni kayıtlar bundan sonra doğrulama bağlantısı alacak."
