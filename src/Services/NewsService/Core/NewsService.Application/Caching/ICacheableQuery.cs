namespace NewsService.Application.Caching;

public interface ICacheableQuery
{
    string CacheKey { get; }

    TimeSpan Duration { get; }
}
