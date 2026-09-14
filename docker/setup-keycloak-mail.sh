#!/usr/bin/env bash
#
# Keycloak'ın "şifremi unuttum" akışı için SMTP ayarlarını yapar.
#
# NEDEN AYRI BİR BETİK: SMTP parolası bir sır ve realm dosyası repoda duruyor.
# Ayar oraya yazılsaydı Gmail uygulama parolası git geçmişine girerdi. Parola
# .env'de kalıyor, buraya oradan okunuyor.
#
# resetPasswordAllowed bayrağı sır değil ve realm JSON'ında da tanımlı — ama
# realm import YALNIZCA ilk açılışta uygulanıyor. Zaten ayakta olan bir kurulumda
# dosyayı değiştirmek hiçbir şey yapmıyor. Bu yüzden betik bayrağı da ayarlıyor:
# JSON sıfırdan kurulumu, betik mevcut kurulumu hizaya getiriyor.
#
# Tekrar çalıştırılabilir: ayarı her seferinde üzerine yazıyor.
#
# Kullanım:  ./docker/setup-keycloak-mail.sh
# Ön koşul:  .env'de SMTP_USERNAME ve SMTP_PASSWORD dolu olmalı.

set -euo pipefail

cd "$(dirname "$0")/.."

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
  -d "grant_type=password" \
  | python3 -c "import sys,json;print(json.load(sys.stdin)['access_token'])")

# Mevcut realm ayarları okunup ÜZERİNE ekleniyor. Keycloak'ın realm güncelleme
# ucu kısmi gövde kabul ediyor ama alanları sessizce sıfırlayabildiği için
# tam nesne gönderiliyor — resetPasswordAllowed gibi başka bayraklar kaybolmasın.
echo "Realm ayarlanıyor (resetPasswordAllowed + SMTP + olay kaydı)..."

python3 - "$KC" "$REALM" "$TOKEN" "$SMTP_USERNAME" "$SMTP_PASSWORD" <<'PY'
import json, sys, urllib.request

kc, realm, token, user, password = sys.argv[1:6]

def call(method, path, body=None):
    req = urllib.request.Request(
        f"{kc}{path}", method=method,
        data=json.dumps(body).encode() if body is not None else None,
        headers={"Authorization": f"Bearer {token}", "Content-Type": "application/json"})
    with urllib.request.urlopen(req) as response:
        raw = response.read()
        return json.loads(raw) if raw else None

current = call("GET", f"/admin/realms/{realm}")

# Realm import yalnızca ilk açılışta çalıştığı için, ayakta olan kurulumda bu
# bayrak kapalı kalıyor. Giriş ekranındaki bağlantı açık olmadan çalışmıyor.
current["resetPasswordAllowed"] = True

current["smtpServer"] = {
    "host": "smtp.gmail.com",
    "port": "587",
    "from": user,
    "fromDisplayName": "Telgraf",
    "replyTo": user,
    "ssl": "false",
    # Gmail 587'de STARTTLS istiyor; "ssl" (465, örtük TLS) ile karıştırılmamalı.
    "starttls": "true",
    "auth": "true",
    "user": user,
    "password": password,
}

# Sıfırlama bağlantısının ömrü. Varsayılan 12 saat — bir parola sıfırlama
# bağlantısının o kadar yaşaması gereksiz bir risk; posta kutusuna sonradan
# erişen biri hâlâ kullanabilir. 30 dakika, kullanıcıyı sıkmadan pencereyi
# daraltıyor.
current["actionTokenGeneratedByUserLifespan"] = 1800

call("PUT", f"/admin/realms/{realm}", current)

# Parola olaylarının kaydı. IdentityService'teki yoklayıcı bunları okuyup kendi
# log hattımıza taşıyor; kayıt açık olmazsa okuyacak bir şey bulamaz.
#
# Yalnızca parola türleri: giriş/çıkış olayları saniyede birkaç kez üretilip
# hem Keycloak'ın tablosunu hem logları gürültüye boğardı. Saklama 7 gün —
# kalıcı arşiv Elasticsearch'te, buradaki yalnızca yoklayıcının tamponu.
call("PUT", f"/admin/realms/{realm}/events/config", {
    "eventsEnabled": True,
    "eventsExpiration": 604800,
    "eventsListeners": ["jboss-logging"],
    "enabledEventTypes": [
        "SEND_RESET_PASSWORD", "SEND_RESET_PASSWORD_ERROR",
        "RESET_PASSWORD", "RESET_PASSWORD_ERROR",
        "UPDATE_PASSWORD", "UPDATE_PASSWORD_ERROR",
    ],
    "adminEventsEnabled": False,
})

print("  tamam")
PY

echo
echo "Doğrulanıyor..."
python3 - "$KC" "$REALM" "$TOKEN" <<'PY'
import json, sys, urllib.request
kc, realm, token = sys.argv[1:4]
req = urllib.request.Request(f"{kc}/admin/realms/{realm}",
    headers={"Authorization": f"Bearer {token}"})
d = json.load(urllib.request.urlopen(req))
smtp = d.get("smtpServer") or {}
print("  resetPasswordAllowed :", d.get("resetPasswordAllowed"))
print("  SMTP host            :", smtp.get("host"), "port", smtp.get("port"))
print("  SMTP kullanıcı       :", smtp.get("user"))
print("  parola kaydedildi mi :", "evet" if smtp.get("password") else "HAYIR")
print("  bağlantı ömrü        :", d.get("actionTokenGeneratedByUserLifespan"), "sn")

req = urllib.request.Request(f"{kc}/admin/realms/{realm}/events/config",
    headers={"Authorization": f"Bearer {token}"})
ev = json.load(urllib.request.urlopen(req))
print("  olay kaydı           :", ev.get("eventsEnabled"),
      f"({len(ev.get('enabledEventTypes') or [])} tür)")
PY

echo
echo "Kurulum tamamlandı. Giriş ekranındaki 'Şifremi unuttum' bağlantısı artık çalışıyor."
