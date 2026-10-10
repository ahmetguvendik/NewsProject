import { Link, useLocation } from 'react-router-dom'

/**
 * Hiçbir route ile eşleşmeyen adresler buraya düşer.
 *
 * Catch-all olmadan React hiçbir şey render etmiyordu: yanlış yazılmış ya da
 * eskimiş bir bağlantı simsiyah, tek karakter metni olmayan bir sayfa veriyordu.
 * Yetkisiz sayfa için zaten düzgün bir boş durum ekranı vardı (bkz. Guard),
 * bulunamayan sayfa için yoktu.
 *
 * Denenen adres gösteriliyor: kullanıcı yazım hatasını kendi görebilsin, bize
 * bildirecekse de neyi bildireceğini bilsin.
 */
export function NotFoundPage() {
  const location = useLocation()

  return (
    <div className="empty">
      <h1 className="empty__title">Sayfa bulunamadı</h1>
      <p>
        <code>{location.pathname}</code> adresinde bir sayfa yok. Bağlantı eskimiş
        ya da adres yanlış yazılmış olabilir.
      </p>
      <p>
        <Link className="btn" to="/">← Akışa dön</Link>
      </p>
    </div>
  )
}
