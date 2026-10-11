import { ApiError } from '../api/http'

/**
 * Backend'in standart ErrorResponse gövdesini gösterir:
 * kısa mesaj + açıklama + alan hataları + makine okunur errorCode.
 *
 * `message` neyin olmadığını söylüyor ("Bu kategori silinemez."), `description`
 * ise ne yapılacağını ("...bağlı 7 haber var, önce başka kategoriye taşıyın").
 * İkisi ayrı alan çünkü backend de ayrı üretiyor; açıklamayı basmazsak
 * kullanıcıya kalan tek bilgi işe yaramaz oluyor.
 */
export function ErrorAlert({ error }: { error: unknown }) {
  if (!error) return null

  if (error instanceof ApiError) {
    // Development ortamında 5xx için backend, description'a yönlendirme metni
    // değil yığın izini koyuyor (bkz. GlobalExceptionHandler). Onu açıklamayla
    // aynı yerde basmak kutuyu okunmaz hale getirir; katlanır bloğa alınıyor.
    const isStackTrace = error.status >= 500 && error.description.includes('\n')

    return (
      // Kod kullanıcıya gösterilmez; hata ayıklarken üzerine gelince görünür.
      <div className="alert alert--error" title={`HTTP ${error.status} · ${error.errorCode}`}>
        <strong>{error.message}</strong>
        {error.description && !isStackTrace && <p className="alert__hint">{error.description}</p>}
        {error.fieldErrors && (
          <ul>
            {/* Alan anahtarı (Password, Title...) gösterilmez; mesajlar alanı zaten Türkçe adıyla anıyor. */}
            {Object.entries(error.fieldErrors).map(([field, messages]) => (
              <li key={field}>{messages.join(' ')}</li>
            ))}
          </ul>
        )}
        {isStackTrace && (
          <details className="alert__trace">
            <summary>Yığın izi</summary>
            <pre>{error.description}</pre>
          </details>
        )}
      </div>
    )
  }

  return <div className="alert alert--error">{error instanceof Error ? error.message : String(error)}</div>
}
