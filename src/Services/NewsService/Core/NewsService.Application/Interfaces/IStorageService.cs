namespace NewsService.Application.Interfaces;

/// <summary>
/// Medya dosyalarının saklandığı nesne deposu (yerelde MinIO, üretimde S3).
///
/// Dosya baytları hiçbir zaman bu servisten geçmez: client imzalı bir URL ile
/// doğrudan depoya yükler. Backend yalnızca izin verir ve sonucu doğrular —
/// böylece büyük dosyalar uygulama belleğini ve gateway gövde limitini zorlamaz.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Beyan edilen tip/boyut politikaya uyuyorsa geçici (staging) bir anahtar
    /// üretip o anahtara yazma izni veren kısa ömürlü imzalı URL döndürür.
    /// </summary>
    PresignedUpload CreateUploadUrl(string contentType, long sizeBytes);

    /// <summary>
    /// Yüklenen dosyayı gerçek içeriğine bakarak doğrular ve kalıcı alana taşır.
    /// Doğrulamayı geçemeyen dosya silinir. Döndürülen değer veritabanına yazılacak anahtardır.
    /// </summary>
    Task<string> CommitAsync(string stagingKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Veritabanındaki değeri okunabilir bir adrese çevirir. Mutlak URL'ler
    /// (dışarıdan yapıştırılmış adresler) olduğu gibi geçer; anahtarlar
    /// depo adresiyle birleştirilir.
    /// </summary>
    string? ResolvePublicUrl(string? storedValue);

    /// <summary>
    /// <c>srcset</c> değeri üretir; tarayıcı ekranda kapladığı yere göre doğru
    /// boyu seçebilsin diye. Dış adreslerde ve varyantı olmayan eski kayıtlarda
    /// <c>null</c> döner — o durumda tek adres kullanılır.
    /// </summary>
    string? ResolveSrcset(string? storedValue);
}

/// <param name="UploadUrl">Client'ın PUT isteği atacağı imzalı adres.</param>
/// <param name="Key">Yükleme bittikten sonra commit'e gönderilecek geçici anahtar.</param>
/// <param name="ContentType">İmzaya dahil edilen tip — client aynısını göndermek zorunda.</param>
/// <param name="ExpiresInSeconds">URL'in geçerlilik süresi.</param>
public record PresignedUpload(string UploadUrl, string Key, string ContentType, int ExpiresInSeconds);
