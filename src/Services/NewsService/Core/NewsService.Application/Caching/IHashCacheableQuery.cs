namespace NewsService.Application.Caching;

public interface IHashCacheableQuery
{
    string HashKey { get; }

    // Yanıtı değiştiren her parametreyi (taslak görünürlüğü dahil) içermeli; null ise önbelleklenmez.
    string? Field { get; }

    TimeSpan Duration { get; }
}
