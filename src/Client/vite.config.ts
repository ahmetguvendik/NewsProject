import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// identity-service ve news-service artık kendi portlarını dışarı açmıyor —
// tek giriş noktası APISIX gateway'i (:9080). Path'ler servisler arasında
// çakışmadığı için (bkz. docker/apisix/setup-routes.sh) client hangi backend'e
// gittiğini bilmek zorunda değil, tek proxy hedefi yeterli.
// Keycloak ayrı kalıyor: admin console kendi portunda (8080), login akışı da
// oraya doğrudan gidiyor.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      // Vite proxy path eşleşmesini obje sırasına göre yapıyor (path.startsWith,
      // ilk eşleşen kazanır) — "/gw/kc" "/gw"'nin bir öneki olduğu için MUTLAKA
      // önce tanımlanmalı. Sıra tersine çevrilirse Keycloak istekleri de "/gw"
      // kuralına düşer ve yanlışlıkla APISIX'e gider.
      '/gw/kc': {
        target: 'http://localhost:8080',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/gw\/kc/, ''),
      },
      '/gw': {
        target: 'http://localhost:9080',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/gw/, ''),
      },
    },
  },
})
