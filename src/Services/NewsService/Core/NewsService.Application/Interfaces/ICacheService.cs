namespace NewsService.Application.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken cancellationToken = default) where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task<T?> GetHashFieldAsync<T>(string key, string field, CancellationToken cancellationToken = default) where T : class;

    Task SetHashFieldAsync<T>(string key, string field, T value, TimeSpan duration, CancellationToken cancellationToken = default)
        where T : class;
}
