/**
 * Haberlerin çoğunun görseli olmadığı için kapak alanı boş kalmasın diye
 * kategori adından deterministik bir gradyan üretilir: aynı kategori her
 * zaman aynı rengi alır, böylece akış rastgele değil düzenli görünür.
 *
 * Görseli olan haberlerde gradyan üretilmez; kapağı <CoverImage> çizer.
 * Görsel artık CSS `background-image` ile değil `<img srcset>` ile
 * konuyor: background-image tek adres alır, `image-set()` ise yalnızca
 * çözünürlük çarpanını bilir — görselin ekranda kaç piksel kapladığını
 * bilmediği için doğru boyu seçemez.
 */
export function coverStyle(imageUrl: string | null, seed: string) {
  if (imageUrl) return undefined

  const hue = hashHue(seed)

  return {
    backgroundImage:
      `radial-gradient(120% 120% at 18% 12%, hsl(${hue} 62% 32%) 0%, hsl(${(hue + 34) % 360} 54% 18%) 55%, #12151a 100%)`,
  }
}

function hashHue(value: string) {
  let hash = 0
  for (let i = 0; i < value.length; i++) {
    hash = (hash * 31 + value.charCodeAt(i)) | 0
  }
  return Math.abs(hash) % 360
}
