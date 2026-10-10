namespace NewsService.Application.Media;

public static class MediaPolicy
{
    public const long MaxSizeBytes = 5 * 1024 * 1024;

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
