import { api } from './http'

/**
 * Piyasa şeridi verisi. Hava durumuyla aynı gerekçeyle kendi backend'imizden
 * geliyor: dört ayrı sembol çekiliyor ve her ziyaretçinin tarayıcısı doğrudan
 * Yahoo'ya gitseydi hem dört kat istek olurdu hem de rate limit riski.
 * Sunucuda bir dakika önbellekleniyor.
 */
export interface MarketQuote {
  label: string
  value: number
  /** Önceki kapanışa göre yüzde değişim; yön ve renk buradan belirleniyor. */
  changePercent: number
}

export interface MarketSnapshot {
  quotes: MarketQuote[]
  fetchedAt: string
}

export const fetchMarket = () => api.get<MarketSnapshot>('/api/market')
