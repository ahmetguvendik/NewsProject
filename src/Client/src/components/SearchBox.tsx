import { useEffect, useState } from 'react'
import type { FormEvent, KeyboardEvent } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'

/**
 * Backend'de arama endpoint'i yok — bu yüzden akıştaki (zaten yüklenmiş,
 * yayınlanmış) haberler üzerinde başlık/özet filtresi yapıyoruz. Aramaya
 * basınca "/?q=..." adresine gidiliyor, ArticlesPage bu parametreyi okuyup
 * filtreliyor.
 */
export function SearchBox() {
  const navigate = useNavigate()
  const [params] = useSearchParams()
  const [term, setTerm] = useState(params.get('q') ?? '')

  // "q" başka bir yerden (ör. ArticlesPage'deki "temizle" butonu) değişirse
  // kutunun içeriği senkron kalsın.
  useEffect(() => {
    setTerm(params.get('q') ?? '')
  }, [params])

  const runSearch = () => {
    const trimmed = term.trim()
    navigate(trimmed ? `/?q=${encodeURIComponent(trimmed)}` : '/')
  }

  const handleSubmit = (event: FormEvent) => {
    event.preventDefault()
    runSearch()
  }

  // type="search" input'larda Enter'ın form submit'ini tetiklemesi bazı
  // tarayıcı/otomasyon bağlamlarında güvenilir değil — elle de yakalıyoruz.
  const handleKeyDown = (event: KeyboardEvent<HTMLInputElement>) => {
    if (event.key === 'Enter') {
      event.preventDefault()
      runSearch()
    }
  }

  return (
    <form className="search-box" onSubmit={handleSubmit} role="search">
      <input
        className="search-box__input"
        type="search"
        placeholder="Haber ara…"
        value={term}
        onChange={(event) => setTerm(event.target.value)}
        onKeyDown={handleKeyDown}
        aria-label="Haber ara"
      />
      <button className="search-box__btn" type="submit" aria-label="Ara">
        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
          <circle cx="11" cy="11" r="7" />
          <line x1="21" y1="21" x2="16.65" y2="16.65" />
        </svg>
      </button>
    </form>
  )
}
