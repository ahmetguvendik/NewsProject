namespace NewsService.Application.Interfaces;

/// <summary>
/// Dağıtık önbellek (Redis) sözleşmesi.
///
/// Önbellek bir hızlandırıcıdır, doğruluk kaynağı değil: uygulama bu servise
/// erişilemediğinde de çalışmaya devam etmelidir. Bu yüzden okuma başarısız
/// olduğunda istisna fırlatmak yerine <c>null</c> döner, yazma sessizce atlanır.
/// </summary>
public interface ICacheService
{
    /// <summary>Önbellekte yoksa veya önbelleğe ulaşılamıyorsa <c>null</c> döner.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken cancellationToken = default) where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
