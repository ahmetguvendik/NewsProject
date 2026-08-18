namespace IdentityService.Application.Interfaces;

/// <summary>
/// Dağıtık önbellek (Redis) sözleşmesi.
///
/// Önbellek bir hızlandırıcıdır, doğruluk kaynağı değil: uygulama bu servise
/// erişilemediğinde de çalışmaya devam etmelidir. Bu yüzden okuma başarısız
/// olduğunda istisna fırlatmak yerine boş sonuç döner, yazma sessizce atlanır.
///
/// NewsService'in kendi <c>ICacheService</c>'i var ve yüzeyi bununla birebir
/// aynı değil — her servis yalnızca kullandığı işlemleri tanımlıyor. Ortak olan
/// şey sözleşmenin şekli değil, <b>hata karşısındaki davranış</b>: iki tarafta
/// da erişilemeyen önbellek istisnaya değil veritabanına düşmelidir.
/// </summary>
public interface ICacheService
{
    /// <summary>Önbellekte yoksa veya önbelleğe ulaşılamıyorsa <c>null</c> döner.</summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken cancellationToken = default) where T : class;

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Bir hash'ten birden çok alanı tek turda okur. Bulunamayan alanlar dönen
    /// sözlükte hiç yer almaz — çağıran taraf eksikleri kendi kaynağından
    /// tamamlar. Önbelleğe erişilemezse boş sözlük döner, yani her şey ıska
    /// sayılır ve akış veritabanına düşer.
    /// </summary>
    Task<IReadOnlyDictionary<string, T>> GetHashFieldsAsync<T>(
        string key,
        IReadOnlyCollection<string> fields,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Birden çok alanı tek turda yazar. Süre alan bazında değil hash'in
    /// tamamına uygulanır (Redis'te alan bazlı TTL yok).
    /// </summary>
    Task SetHashFieldsAsync<T>(
        string key,
        IReadOnlyDictionary<string, T> values,
        TimeSpan duration,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Tek bir alanı düşürür. Hash'in tamamını silmek yerine bu kullanılır:
    /// tek kullanıcı değiştiğinde bütün dizinin isabetini çöpe atmak,
    /// önbelleği koymanın amacını ortadan kaldırır.
    /// </summary>
    Task RemoveHashFieldAsync(string key, string field, CancellationToken cancellationToken = default);
}
