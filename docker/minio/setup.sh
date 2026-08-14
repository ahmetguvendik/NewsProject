#!/bin/sh
# MinIO bucket kurulumu. Tekrar tekrar çalıştırılabilir: her adım "zaten var"
# durumunu hatasız geçer, böylece compose her ayağa kalkışta güvenle çağırabilir.
set -e

mc alias set local http://minio:9000 "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD"

mc mb --ignore-existing "local/$MINIO_BUCKET"

# Haber görselleri herkese açık okunur olmalı: aksi halde her görüntüleme için
# imzalı GET üretmek gerekir, bu da CDN/tarayıcı önbelleğini işlevsiz kılar.
# Yazma izni yok — yükleme yalnızca backend'in ürettiği imzalı URL ile yapılır.
mc anonymous set download "local/$MINIO_BUCKET"

# Makaleye bağlanmadan yarım kalan yüklemeler çöp olarak birikmesin.
cat > /tmp/lifecycle.json <<JSON
{
  "Rules": [
    {
      "ID": "expire-staging",
      "Status": "Enabled",
      "Filter": { "Prefix": "staging/" },
      "Expiration": { "Days": 1 }
    }
  ]
}
JSON
mc ilm import "local/$MINIO_BUCKET" < /tmp/lifecycle.json

echo "MinIO hazır: bucket=$MINIO_BUCKET (public read, staging/ 1 gün sonra silinir)"
mc ilm ls "local/$MINIO_BUCKET" || true
