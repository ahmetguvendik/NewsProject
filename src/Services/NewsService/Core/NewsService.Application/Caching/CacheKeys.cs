namespace NewsService.Application.Caching;

/// <summary>
/// Önbellek anahtarları tek yerde tutulur: okuyan sorgu ile geçersiz kılan
/// komut aynı sabiti kullanmazsa, yazma sonrası bayat veri servis edilir ve
/// bu tür bir hata sessizce ilerler.
/// </summary>
public static class CacheKeys
{
    public const string Categories = "news:categories";
    public const string Tags = "news:tags";
    public const string Weather = "news:weather";
}
