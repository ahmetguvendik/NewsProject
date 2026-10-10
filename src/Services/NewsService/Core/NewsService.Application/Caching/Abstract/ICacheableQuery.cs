namespace NewsService.Application.Caching.Abstract;

public interface ICacheableQuery
{
    string CacheKey { get; }

    TimeSpan Duration { get; }
}
