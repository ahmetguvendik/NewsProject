using System.Text.Json;
using IdentityService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace IdentityService.Persistance.Caching;

/// <summary>
/// Redis tabanlı önbellek.
///
/// Tüm işlemler hataya karşı yutucudur: Redis erişilemezse okuma boş döner ve
/// çağıran taraf veritabanına düşer, yazma sessizce atlanır. Önbellek bir
/// hızlandırıcı olduğu için erişilemez olması siteyi düşürmemeli.
///
/// <b>NewsService.Persistance/Caching/RedisCacheService.cs bunun ikizidir.</b>
/// Metot listeleri farklı (her servis yalnızca kullandığını tanımlıyor) ama
/// hata karşısındaki davranış — istisna yutma, devre kesici, hash TTL'inin
/// yalnızca ilk oluşturmada kurulması — aynı olmak zorunda. Bu davranışı
/// değiştiren bir düzeltme yaparken diğer dosyaya da bakılmalı; tek tarafta
/// kalırsa "Redis kapalıyken servis ayakta kalır" garantisi orada sessizce
/// bozulur.
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Redis erişilemez olduğunda her istekte yeniden bağlanmayı denemek, bağlantı
    /// zaman aşımı kadar gecikme ekler ve site fiilen kullanılamaz hale gelir.
    /// Bir hata alındıktan sonra bu süre boyunca Redis'e hiç dokunulmaz; istekler
    /// doğrudan veritabanına gider.
    /// </summary>
    private readonly TimeSpan _circuitOpenDuration;

    private static long _skipUntilTicks;

    private static bool IsCircuitOpen => Interlocked.Read(ref _skipUntilTicks) > DateTime.UtcNow.Ticks;

    private void OpenCircuit(Exception ex, string operation, string key)
    {
        Interlocked.Exchange(ref _skipUntilTicks, DateTime.UtcNow.Add(_circuitOpenDuration).Ticks);
        _logger.LogWarning(ex, "Önbellek erişilemiyor ({Operation}: {Key}); {Seconds} sn atlanacak.",
            operation, key, _circuitOpenDuration.TotalSeconds);
    }

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger, IConfiguration configuration)
    {
        _redis = redis;
        _logger = logger;
        _circuitOpenDuration = TimeSpan.FromSeconds(configuration.GetValue("Redis:CircuitOpenSeconds", 10));
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (IsCircuitOpen)
            return null;

        try
        {
            var value = await _redis.GetDatabase().StringGetAsync(key);
            if (value.IsNullOrEmpty)
                return null;

            // RedisValue hem string'e hem ReadOnlySpan<byte>'a örtük dönüştüğü için
            // aşırı yükleme belirsiz kalıyor; tür açıkça belirtiliyor.
            return JsonSerializer.Deserialize<T>((string)value!, SerializerOptions);
        }
        catch (Exception ex) when (ex is RedisException or JsonException)
        {
            // Bozuk kayıt veya erişilemeyen Redis → veritabanına düşülür.
            if (ex is RedisException) OpenCircuit(ex, "okuma", key);
            else _logger.LogWarning(ex, "Önbellekteki kayıt çözümlenemedi: {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan duration, CancellationToken cancellationToken = default)
        where T : class
    {
        if (IsCircuitOpen)
            return;

        try
        {
            var payload = JsonSerializer.Serialize(value, SerializerOptions);
            await _redis.GetDatabase().StringSetAsync(key, payload, duration);
        }
        catch (Exception ex) when (ex is RedisException or JsonException)
        {
            if (ex is RedisException) OpenCircuit(ex, "yazma", key);
            else _logger.LogWarning(ex, "Önbelleğe yazılamadı: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (IsCircuitOpen)
            return;

        try
        {
            await _redis.GetDatabase().KeyDeleteAsync(key);
        }
        catch (RedisException ex)
        {
            // Silinemeyen anahtar bayat veri demek; TTL sınırı olduğu için
            // kalıcı değil, ama görünür olması gerekiyor.
            OpenCircuit(ex, "silme", key);
        }
    }

    public async Task<IReadOnlyDictionary<string, T>> GetHashFieldsAsync<T>(
        string key,
        IReadOnlyCollection<string> fields,
        CancellationToken cancellationToken = default) where T : class
    {
        var found = new Dictionary<string, T>(fields.Count);

        if (IsCircuitOpen || fields.Count == 0)
            return found;

        var names = fields.Select(field => (RedisValue)field).ToArray();

        try
        {
            var values = await _redis.GetDatabase().HashGetAsync(key, names);

            // HashGetAsync sırayı korur ve bulunamayan alanlar için boş değer
            // döner; indeks eşlemesi bu yüzden güvenli.
            for (var i = 0; i < names.Length; i++)
            {
                if (values[i].IsNullOrEmpty)
                    continue;

                var item = JsonSerializer.Deserialize<T>((string)values[i]!, SerializerOptions);
                if (item is not null)
                    found[(string)names[i]!] = item;
            }
        }
        catch (Exception ex) when (ex is RedisException or JsonException)
        {
            // Kısmi sonuç dönmek yerine elde ne varsa onunla devam edilir:
            // eksik alanlar ıska sayılıp veritabanından tamamlanacak.
            if (ex is RedisException) OpenCircuit(ex, "hash okuma", key);
            else _logger.LogWarning(ex, "Önbellekteki kayıt çözümlenemedi: {Key}", key);
        }

        return found;
    }

    public async Task SetHashFieldsAsync<T>(
        string key,
        IReadOnlyDictionary<string, T> values,
        TimeSpan duration,
        CancellationToken cancellationToken = default) where T : class
    {
        if (IsCircuitOpen || values.Count == 0)
            return;

        try
        {
            var entries = values
                .Select(pair => new HashEntry(pair.Key, JsonSerializer.Serialize(pair.Value, SerializerOptions)))
                .ToArray();

            var database = _redis.GetDatabase();
            await database.HashSetAsync(key, entries);

            // Redis'te alan bazlı TTL yok; süre tüm hash'e uygulanır. HasNoExpiry
            // sayesinde süre yalnızca hash ilk oluşturulduğunda kurulur — her yeni
            // alanda tazelenseydi, sürekli trafik altında hash hiç sona ermezdi.
            await database.KeyExpireAsync(key, duration, ExpireWhen.HasNoExpiry);
        }
        catch (Exception ex) when (ex is RedisException or JsonException)
        {
            if (ex is RedisException) OpenCircuit(ex, "hash yazma", key);
            else _logger.LogWarning(ex, "Önbelleğe yazılamadı: {Key}", key);
        }
    }

    public async Task RemoveHashFieldAsync(string key, string field, CancellationToken cancellationToken = default)
    {
        if (IsCircuitOpen)
            return;

        try
        {
            await _redis.GetDatabase().HashDeleteAsync(key, field);
        }
        catch (RedisException ex)
        {
            OpenCircuit(ex, "hash silme", $"{key}/{field}");
        }
    }
}
