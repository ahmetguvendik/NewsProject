interface CoverImageProps {
  /** Çözümlenmiş kapak adresi; null ise kapak alanında gradyan kalır. */
  url: string | null
  /** Backend'in ürettiği boy listesi. Dış adreslerde ve eski kayıtlarda null. */
  srcset?: string | null
  /**
   * Görselin ekranda kaplayacağı genişlik. Tarayıcı indireceği boyu buna göre
   * seçtiği için düzenle uyumlu olması gerekir; yanlış değer ya bulanık görsele
   * ya da gereksiz büyük indirmeye yol açar.
   */
  sizes: string
  /** Ekranın en üstündeki kapaklar (manşet, detay) beklemesin diye kapatılır. */
  lazy?: boolean
}

/**
 * Kapak görseli. Kapsayıcıyı tamamen doldurur; üstündeki chip ve karartma
 * katmanı CSS'te bunun üzerinde kalır.
 */
export function CoverImage({ url, srcset, sizes, lazy = true }: CoverImageProps) {
  if (!url) return null

  return (
    <img
      className="cover__img"
      src={url}
      // srcset yokken sizes vermek anlamsız; tarayıcı zaten tek adres indirir.
      srcSet={srcset ?? undefined}
      sizes={srcset ? sizes : undefined}
      // Kapak dekoratif: haberin adı hemen yanındaki başlık bağlantısında geçiyor,
      // alt metni doldurmak ekran okuyucuda aynı bilgiyi iki kez okuturdu.
      alt=""
      loading={lazy ? 'lazy' : 'eager'}
      decoding="async"
    />
  )
}
