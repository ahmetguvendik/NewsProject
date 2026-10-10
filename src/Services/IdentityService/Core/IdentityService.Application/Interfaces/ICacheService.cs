namespace IdentityService.Application.Interfaces;

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken cancellationToken = default) where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, T>> GetHashFieldsAsync<T>(
        string key,
        IReadOnlyCollection<string> fields,
        CancellationToken cancellationToken = default) where T : class;

    Task SetHashFieldsAsync<T>(
        string key,
        IReadOnlyDictionary<string, T> values,
        TimeSpan duration,
        CancellationToken cancellationToken = default) where T : class;

    Task RemoveHashFieldAsync(string key, string field, CancellationToken cancellationToken = default);
}
