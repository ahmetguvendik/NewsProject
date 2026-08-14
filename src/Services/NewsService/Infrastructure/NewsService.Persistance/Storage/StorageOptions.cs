namespace NewsService.Persistance.Storage;

public class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>Konteyner ağından erişilen adres — sunucu tarafı işlemler (kontrol, taşıma, silme) buradan gider.</summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// Tarayıcının erişebildiği adres. İmzalı URL'ler <b>yalnızca</b> bununla üretilir:
    /// imza host'u da kapsadığı için iç ağ adıyla ("minio:9000") imzalanan bir URL'i
    /// tarayıcı çözemez, host'u sonradan düzeltmek de imzayı geçersiz kılar.
    /// </summary>
    public string PublicEndpoint { get; set; } = string.Empty;

    public string Bucket { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>İmzalı yükleme URL'inin ömrü. Kısa tutulur; yükleme hemen başlar.</summary>
    public int UploadUrlTtlSeconds { get; set; } = 300;
}
