namespace NewsService.Application.Media;

/// <summary>
/// Yüklenebilecek medyanın tek doğruluk kaynağı. Aynı kurallar iki kez uygulanır:
/// imzalı URL üretilirken client'ın <em>beyan ettiği</em> tip/boyuta, yükleme
/// tamamlandıktan sonra da depodaki <em>gerçek</em> dosyaya karşı.
/// </summary>
public static class MediaPolicy
{
    /// <summary>
    /// 5 MB. Normal bir kapak görseli ~500 KB; bu sınır işlenmemiş fotoğraf
    /// yüklemelerine yer bırakırken mobil kullanıcıyı devasa dosyalardan korur.
    /// </summary>
    public const long MaxSizeBytes = 5 * 1024 * 1024;

    /// <summary>
    /// İzinli tipler ve depoda kullanılacak uzantıları.
    /// Bilerek dışarıda bırakılanlar:
    /// <list type="bullet">
    /// <item>SVG — içinde script taşıyabilir, yüklenen dosya XSS'e dönüşür.</item>
    /// <item>HEIC — iPhone varsayılanı ama hiçbir tarayıcı gösteremez.</item>
    /// <item>GIF — aynı animasyon için WebP/MP4'ün birkaç katı boyut.</item>
    /// </list>
    /// </summary>
    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/avif"] = ".avif",
    };

    public static IReadOnlyCollection<string> AllowedContentTypes => Extensions.Keys;

    public static bool IsAllowed(string? contentType) =>
        !string.IsNullOrWhiteSpace(contentType) && Extensions.ContainsKey(contentType);

    public static string ExtensionFor(string contentType) => Extensions[contentType];

    /// <summary>
    /// Dosyanın ilk baytlarından gerçek türünü doğrular. Client'ın gönderdiği
    /// Content-Type başlığı serbestçe uydurulabildiği için asıl kontrol budur:
    /// ".jpg" adıyla ve "image/jpeg" başlığıyla gönderilen bir HTML dosyası buradan geçemez.
    /// </summary>
    public static bool MatchesMagicBytes(string contentType, ReadOnlySpan<byte> head) =>
        contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => head.Length >= 3
                && head[0] == 0xFF && head[1] == 0xD8 && head[2] == 0xFF,

            "image/png" => head.Length >= 8
                && head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47,

            // RIFF....WEBP — boyut alanı 4-8 arasında olduğu için atlanır.
            "image/webp" => head.Length >= 12
                && head[..4].SequenceEqual("RIFF"u8) && head[8..12].SequenceEqual("WEBP"u8),

            // ISO-BMFF kutusu: 4 bayt uzunluk + "ftyp" + marka.
            "image/avif" => head.Length >= 12
                && head[4..8].SequenceEqual("ftyp"u8),

            _ => false,
        };
}
