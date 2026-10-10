namespace NewsService.Application.Media;

public static class MediaVariants
{
    public static readonly int[] Widths = [400, 800, 1200];

    public const int DefaultWidth = 800;

    public const string Extension = ".webp";

    public static string FileKey(string baseKey, int width) => $"{baseKey}-{width}{Extension}";

    public static bool HasVariants(string key) => !Path.HasExtension(key);
}
