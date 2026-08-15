namespace NewsService.Application.Caching;

/// <summary>
/// Aynı anahtar altında çok sayıda varyantı olan sorgular için. Makale listesi
/// böyle: sayfa, kategori ve rol kombinasyonlarının her biri ayrı bir yanıt
/// üretiyor ve bir haber değiştiğinde <b>hepsi</b> birden bayatlıyor.
///
/// Varyantlar tek hash'in alanları olduğu için tamamı bir <c>DEL</c> ile
/// düşürülebiliyor; ayrı anahtarlar kullanılsaydı desenle tarayıp silmek
/// gerekirdi (pahalı ve yarış koşullu).
/// </summary>
public interface IHashCacheableQuery
{
    string HashKey { get; }

    /// <summary>
    /// Varyantı tanımlayan alan adı. Yanıtı değiştiren <b>her</b> parametreyi
    /// içermeli — özellikle taslakların görünüp görünmediğini, aksi halde
    /// yetkili bir kullanıcının yanıtı anonim ziyaretçiye servis edilir.
    ///
    /// <c>null</c> dönerse bu istek önbelleklenmez.
    /// </summary>
    string? Field { get; }

    TimeSpan Duration { get; }
}
