#!/usr/bin/env bash
#
# Keycloak'ın e-posta bağlı realm ayarlarını yapar: SMTP, "şifremi unuttum",
# kayıtta e-posta doğrulama ve parola olaylarının kaydı.
#
# NEDEN AYRI BİR BETİK: SMTP parolası bir sır ve realm dosyası repoda duruyor.
# Ayar oraya yazılsaydı Gmail uygulama parolası git geçmişine girerdi. Parola
# .env'de kalıyor, buraya oradan okunuyor.
#
# resetPasswordAllowed ve verifyEmail bayrakları sır değil ve realm JSON'ında da
# tanımlı — ama realm import YALNIZCA ilk açılışta uygulanıyor. Zaten ayakta olan
# bir kurulumda dosyayı değiştirmek hiçbir şey yapmıyor. Bu yüzden betik onları da
# ayarlıyor: JSON sıfırdan kurulumu, betik mevcut kurulumu hizaya getiriyor.
# (Aynı bölünme docker/setup-keycloak-client.sh için de geçerli.)
#
# JSON işleri jq ile: betik eskiden python3 kullanıyordu ve geliştirme
# makinelerinde Xcode lisans engeline takılıp sessizce çalışmaz hale geliyordu.
# jq bu depodaki diğer betiklerin de kullandığı araç.
#
# Tekrar çalıştırılabilir: ayarları her seferinde üzerine yazıyor.
#
# Kullanım:  ./docker/setup-keycloak-mail.sh
# Ön koşul:  .env'de SMTP_USERNAME ve SMTP_PASSWORD dolu olmalı.

set -euo pipefail

cd "$(dirname "$0")/.."

JQ="${JQ:-jq}"
command -v "$JQ" >/dev/null || { echo "HATA: jq bulunamadı." >&2; exit 1; }

if [ ! -f .env ]; then
  echo "HATA: .env bulunamadı." >&2
  exit 1
fi

# .env `source` EDİLMİYOR: bazı değerler boşluk içeriyor (Gmail uygulama parolası
# "xxxx yyyy zzzz" biçiminde) ve source bunu komut olarak çalıştırmaya kalkıyor.
read_env() {
  local value
  value=$(grep -m1 "^$1=" .env | cut -d= -f2-)
  if [ -z "$value" ]; then
    echo "HATA: .env dosyasında $1 tanımlı değil." >&2
    exit 1
  fi
  printf '%s' "$value"
}

SMTP_USERNAME=$(read_env SMTP_USERNAME)
SMTP_PASSWORD=$(read_env SMTP_PASSWORD)

KC="${KEYCLOAK_URL:-http://localhost:8080}"
REALM="${KEYCLOAK_REALM:-news-portal}"
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

api() { curl -s -H "Authorization: Bearer $TOKEN" "$@"; }

TMP=$(mktemp -d)
trap 'rm -rf "$TMP"' EXIT

# ─── Doğrulanmamış hesapların kilitlenmesi ────────────────────────────
# verifyEmail açıldığı anda e-postası doğrulanmamış HER hesap giriş yapamaz
# hale geliyor (direct grant "invalid_grant / Account is not fully set up").
# Bayrak açılmadan önce uyarılıyor, çünkü bu sessizce olursa mevcut
# kullanıcılar sebebini anlayamadan dışarıda kalır.
#
# Betik kimseyi kendiliğinden "doğrulanmış" işaretlemiyor: bu bir güvenlik
# kararı ve onu sessizce vermek doğru değil. Komut ekrana yazılıyor.
UNVERIFIED=$(api "$KC/admin/realms/$REALM/users?max=1000" \
  | "$JQ" -r '[.[] | select(.emailVerified != true)] | .[].email' || true)

if [ -n "$UNVERIFIED" ]; then
  echo
  echo "UYARI: aşağıdaki hesapların e-postası doğrulanmamış. verifyEmail açıkken"
  echo "bu hesaplar GİRİŞ YAPAMAZ:"
  printf '  - %s\n' $UNVERIFIED
  echo
  echo "Mevcut hesapları doğrulanmış saymak isterseniz (geliştirme ortamı için):"
  echo "  ./docker/verify-existing-users.sh"
  echo
fi

# ─── Realm ayarları ───────────────────────────────────────────────────
# Mevcut realm okunup ÜZERİNE ekleniyor. Keycloak'ın realm güncelleme ucu kısmi
# gövde kabul ediyor ama alanları sessizce sıfırlayabildiği için tam nesne
# gönderiliyor — başka bayraklar kaybolmasın.
echo "Realm ayarlanıyor (resetPasswordAllowed + verifyEmail + SMTP + olay kaydı)..."

api "$KC/admin/realms/$REALM" \
  | "$JQ" --arg user "$SMTP_USERNAME" --arg password "$SMTP_PASSWORD" '
      # Realm import yalnızca ilk açılışta çalıştığı için, ayakta olan kurulumda
      # bu bayraklar kapalı kalıyor.
      .resetPasswordAllowed = true

      # Kayıt olan herkesin e-postasına sahip olduğunu kanıtlaması. Sahte hesap
      # seline karşı asıl caydırıcı bu; rate limit yalnızca hızı sınırlıyor.
      | .verifyEmail = true

      | .smtpServer = {
          host: "smtp.gmail.com",
          port: "587",
          from: $user,
          fromDisplayName: "Telgraf",
          replyTo: $user,
          ssl: "false",
          # Gmail 587de STARTTLS istiyor; "ssl" (465, örtük TLS) ile karıştırılmamalı.
          starttls: "true",
          auth: "true",
          user: $user,
          password: $password
        }

      # Sıfırlama ve doğrulama bağlantılarının ömrü. Varsayılan 12 saat — bir
      # parola sıfırlama bağlantısının o kadar yaşaması gereksiz bir risk; posta
      # kutusuna sonradan erişen biri hâlâ kullanabilir. 30 dakika, kullanıcıyı
      # sıkmadan pencereyi daraltıyor.
      | .actionTokenGeneratedByUserLifespan = 1800
    ' > "$TMP/realm.json"

code=$(curl -s -o /dev/null -w '%{http_code}' -X PUT "$KC/admin/realms/$REALM" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  --data-binary @"$TMP/realm.json")
[ "$code" = "204" ] || { echo "HATA: realm güncellenemedi (HTTP $code)." >&2; exit 1; }

# Parola olaylarının kaydı. IdentityService'teki yoklayıcı bunları okuyup kendi
# log hattımıza taşıyor; kayıt açık olmazsa okuyacak bir şey bulamaz.
#
# Yalnızca parola ve doğrulama türleri: giriş/çıkış olayları saniyede birkaç kez
# üretilip hem Keycloak'ın tablosunu hem logları gürültüye boğardı. Saklama
# 7 gün — kalıcı arşiv Elasticsearch'te, buradaki yalnızca yoklayıcının tamponu.
cat > "$TMP/events.json" <<'JSON'
{
  "eventsEnabled": true,
  "eventsExpiration": 604800,
  "eventsListeners": ["jboss-logging"],
  "enabledEventTypes": [
    "SEND_RESET_PASSWORD", "SEND_RESET_PASSWORD_ERROR",
    "RESET_PASSWORD", "RESET_PASSWORD_ERROR",
    "UPDATE_PASSWORD", "UPDATE_PASSWORD_ERROR",
    "SEND_VERIFY_EMAIL", "SEND_VERIFY_EMAIL_ERROR",
    "VERIFY_EMAIL", "VERIFY_EMAIL_ERROR"
  ],
  "adminEventsEnabled": false
}
JSON

code=$(curl -s -o /dev/null -w '%{http_code}' -X PUT "$KC/admin/realms/$REALM/events/config" \
  -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' \
  --data-binary @"$TMP/events.json")
[ "$code" = "204" ] || { echo "HATA: olay ayarı güncellenemedi (HTTP $code)." >&2; exit 1; }

echo "  tamam"
echo
echo "Doğrulanıyor..."

api "$KC/admin/realms/$REALM" | "$JQ" -r '
  "  resetPasswordAllowed : \(.resetPasswordAllowed)",
  "  verifyEmail          : \(.verifyEmail)",
  "  SMTP host            : \(.smtpServer.host) port \(.smtpServer.port)",
  "  SMTP kullanıcı       : \(.smtpServer.user)",
  "  parola kaydedildi mi : \(if (.smtpServer.password // "") == "" then "HAYIR" else "evet" end)",
  "  bağlantı ömrü        : \(.actionTokenGeneratedByUserLifespan) sn"'

api "$KC/admin/realms/$REALM/events/config" | "$JQ" -r '
  "  olay kaydı           : \(.eventsEnabled) (\((.enabledEventTypes // []) | length) tür)"'

echo
echo "Kurulum tamamlandı. 'Şifremi unuttum' ve kayıtta e-posta doğrulama çalışıyor."
