import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Backend servislerinde CORS yapılandırması yok — tarayıcının doğrudan
// localhost:5001/5002'ye gitmesi engellenirdi. Bu yüzden tüm istekler dev
// sunucusunun proxy'si üzerinden aynı origin'den geçiyor.
// Prod'da bu görevi APISIX üstlenmeli (route'ları henüz tanımlı değil).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/gw/identity': {
        target: 'http://localhost:5001',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/gw\/identity/, ''),
      },
      '/gw/news': {
        target: 'http://localhost:5002',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/gw\/news/, ''),
      },
      '/gw/kc': {
        target: 'http://localhost:8080',
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/gw\/kc/, ''),
      },
    },
  },
})
