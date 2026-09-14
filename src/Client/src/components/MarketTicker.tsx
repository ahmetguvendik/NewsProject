import { useEffect, useState } from 'react'
import { fetchMarket, type MarketQuote } from '../api/market'

/** Binlik ayracı ve iki ondalık — 14235.83 yerine "14.235,83". */
const formatValue = (value: number) =>
  value.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

/**
 * Başlığın altındaki piyasa şeridi: BIST 100, dolar, euro, gram altın.
 *
 * Hava durumu widget'ıyla aynı ilke: DEKORATİF. Veri alınamazsa (Yahoo resmî bir
 * API sunmuyor, uç kapanabilir) şerit sessizce gizleniyor ve sayfa hiç bozulmuyor.
 * Bu yüzden hata durumu ekrana yansıtılmıyor.
 */
export function MarketTicker() {
  const [quotes, setQuotes] = useState<MarketQuote[]>([])

  useEffect(() => {
    let cancelled = false

    fetchMarket()
      .then((snapshot) => {
        if (!cancelled) setQuotes(snapshot.quotes)
      })
      .catch(() => {
        // sessizce gizlenir
      })

    return () => {
      cancelled = true
    }
  }, [])

  if (quotes.length === 0) return null

  return (
    <div className="ticker">
      <div className="container ticker__row">
        {quotes.map((quote) => {
          const up = quote.changePercent > 0
          const flat = quote.changePercent === 0

          return (
            <span key={quote.label} className="ticker__item">
              <span className="ticker__label">{quote.label}</span>
              <span className="ticker__value">{formatValue(quote.value)}</span>
              <span className={`ticker__delta${flat ? '' : up ? ' is-up' : ' is-down'}`}>
                {/* Yön yalnızca renkle değil işaretle de veriliyor: renk körlüğünde
                    yeşil ve kırmızı ayırt edilemiyor, ok her koşulda okunuyor. */}
                {flat ? '±' : up ? '▲' : '▼'}
                {Math.abs(quote.changePercent).toFixed(2)}%
              </span>
            </span>
          )
        })}
      </div>
    </div>
  )
}
