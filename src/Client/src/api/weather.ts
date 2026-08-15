import { api } from './http'

/**
 * Hava durumu artık doğrudan Open-Meteo'dan değil, kendi backend'imizden geliyor.
 *
 * Önceden her ziyaretçinin tarayıcısı dış servise ayrı bir istek atıyordu; trafik
 * arttığında rate limit yenip widget'ın herkeste birden kırılması riski vardı.
 * Sunucu tarafında 10 dakika önbelleklendiği için ziyaretçi sayısından bağımsız
 * olarak dış servise 10 dakikada bir istek gidiyor.
 *
 * Gündüz/gece ayrımı da sunucuda yapılıyor — ikon hazır geliyor.
 */
export interface WeatherNow {
  city: string
  temperatureC: number
  icon: string
  description: string
}

export const fetchWeather = () => api.get<WeatherNow>('/api/weather')
