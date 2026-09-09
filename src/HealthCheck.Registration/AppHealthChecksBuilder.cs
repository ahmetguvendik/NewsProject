using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheck.Registration;

/// <summary>
/// Servislerin ihtiyaç duydukları kontrolleri tek tek seçmesini sağlar. Her servis
/// farklı bağımlılıklara sahip: NotificationService Redis kullanmıyor, MinIO yalnızca
/// NewsService'te var. Hepsini otomatik eklemek, kullanılmayan bir bağımlılığın
/// sağlığını raporlamak gibi yanıltıcı bir sonuç üretirdi.
/// </summary>
public sealed class AppHealthChecksBuilder
{
    /// <summary>
    /// Her ağ kontrolüne uygulanan üst sınır.
    ///
    /// ZORUNLU: timeout verilmezse kontrol süresiz bekliyor. Kafka durdurulduğunda
    /// tam bu oldu — kafka kontrolü asılı kaldı, /health ucu 25 saniyede bile cevap
    /// vermedi ve panel "ne bozuk" diyemedi, yalnızca "uç cevap vermedi" dedi.
    /// Aynı servisteki diğer kontroller de görünmez oldu.
    ///
    /// Timeout ile kontrol hızla başarısız oluyor ve HANGİ bağımlılığın çöktüğü
    /// panelde okunabiliyor.
    /// </summary>
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(5);

    private readonly IHealthChecksBuilder _builder;
    private readonly IConfiguration _configuration;

    internal AppHealthChecksBuilder(IHealthChecksBuilder builder, IConfiguration configuration)
    {
        _builder = builder;
        _configuration = configuration;
    }

    /// <summary>
    /// Veritabanı — tek <c>Ready</c> etiketli kontrol. Postgres erişilemezse servis
    /// hiçbir isteği karşılayamaz, dolayısıyla trafik almaması doğru davranış.
    /// </summary>
    public AppHealthChecksBuilder AddPostgres(string connectionName = "DefaultConnection")
    {
        var connectionString = _configuration.GetConnectionString(connectionName);
        if (Missing(connectionString, "postgres", $"ConnectionStrings:{connectionName}", HealthCheckTags.Ready))
            return this;

        _builder.AddNpgSql(
            connectionString,
            name: "postgres",
            failureStatus: HealthStatus.Unhealthy,
            tags: [HealthCheckTags.Ready],
            timeout: CheckTimeout);

        return this;
    }

    /// <summary>
    /// Önbellek. Kesintisi RedisCacheService'teki devre kesici tarafından yutuluyor,
    /// istekler veritabanına düşerek yanıtlanmaya devam ediyor — o yüzden Ready değil.
    /// Yavaşlamanın sebebini görebilmek için yine de raporlanıyor.
    /// </summary>
    public AppHealthChecksBuilder AddRedis(string configKey = "Redis:Connection")
    {
        var connectionString = _configuration[configKey];
        if (Missing(connectionString, "redis", configKey, HealthCheckTags.Dependency))
            return this;

        _builder.AddRedis(
            connectionString,
            name: "redis",
            failureStatus: HealthStatus.Unhealthy,
            tags: [HealthCheckTags.Dependency],
            timeout: CheckTimeout);

        return this;
    }

    /// <summary>
    /// Kafka. WebApi'ler Kafka'ya hiç bağlanmıyor (outbox tabloya yazıyor), yalnızca
    /// consumer barındıran NotificationService için anlamlı. Kesinti outbox sayesinde
    /// tolere edildiğinden Ready değil.
    /// </summary>
    public AppHealthChecksBuilder AddKafka(string configKey = "Kafka:BootstrapServers")
    {
        var bootstrapServers = _configuration[configKey];
        if (Missing(bootstrapServers, "kafka", configKey, HealthCheckTags.Dependency))
            return this;

        _builder.AddKafka(
            new ProducerConfig { BootstrapServers = bootstrapServers },
            name: "kafka",
            failureStatus: HealthStatus.Unhealthy,
            tags: [HealthCheckTags.Dependency],
            timeout: CheckTimeout);

        return this;
    }

    /// <summary>
    /// Keycloak'ın OIDC keşif ucu. Kapalıyken önbelleklenmiş JWKS ile doğrulama bir
    /// süre daha sürebildiği için Ready değil; ama giriş ve kullanıcı yönetimi
    /// çalışmayacağından raporlanması önemli.
    /// </summary>
    public AppHealthChecksBuilder AddKeycloak(string configKey = "Keycloak:Authority")
    {
        var authority = _configuration[configKey];
        if (Missing(authority, "keycloak", configKey, HealthCheckTags.Dependency))
            return this;

        var discoveryUri = new Uri($"{authority.TrimEnd('/')}/.well-known/openid-configuration");

        _builder.AddUrlGroup(
            discoveryUri,
            name: "keycloak",
            failureStatus: HealthStatus.Unhealthy,
            tags: [HealthCheckTags.Dependency],
            timeout: CheckTimeout);

        return this;
    }

    /// <summary>
    /// MinIO'nun kendi sağlık ucu. Yalnızca görsel yükleme/servis etme etkilenir,
    /// haber okuma çalışmaya devam eder — o yüzden Ready değil.
    /// </summary>
    public AppHealthChecksBuilder AddStorage(string configKey = "Storage:Endpoint")
    {
        var endpoint = _configuration[configKey];
        if (Missing(endpoint, "storage", configKey, HealthCheckTags.Dependency))
            return this;

        var liveUri = new Uri($"{endpoint.TrimEnd('/')}/minio/health/live");

        _builder.AddUrlGroup(
            liveUri,
            name: "storage",
            failureStatus: HealthStatus.Unhealthy,
            tags: [HealthCheckTags.Dependency],
            timeout: CheckTimeout);

        return this;
    }

    /// <summary>
    /// Dead-letter'a düşmüş mesaj sayısı. Sıfırdan büyükse Unhealthy.
    ///
    /// ETİKET BİLİNÇLİ OLARAK Dependency, Ready DEĞİL:
    ///   • /health/ready etkilenmiyor → container sağlıklı kalıyor. Ölü bir mesaj
    ///     worker'ın bozuk olduğu anlamına gelmez; döngü çalışmaya devam ediyor ve
    ///     onu yeniden başlatmak hiçbir şeyi düzeltmez.
    ///   • /health Unhealthy oluyor → panel kırmızıya dönüyor ve webhook tetikleniyor.
    ///
    /// Ready etiketi verseydik container unhealthy olur, ona bağlı servisler
    /// beklemeye başlar ve tek bir bozuk mesaj yüzünden çalışan bir sistem
    /// durdurulurdu.
    ///
    /// Sorgu dışarıdan veriliyor: bu proje OutboxMessage/InboxMessage tiplerini
    /// tanımıyor ve tanımamalı.
    /// </summary>
    public AppHealthChecksBuilder AddDeadLetters(
        Func<IServiceProvider, CancellationToken, Task<int>> countAsync)
    {
        _builder.Add(new HealthCheckRegistration(
            name: "dead-letters",
            factory: sp => new DeadLetterHealthCheck(
                sp.GetRequiredService<IServiceScopeFactory>(), countAsync),
            failureStatus: HealthStatus.Unhealthy,
            tags: [HealthCheckTags.Dependency]));

        return this;
    }

    /// <summary>
    /// Worker döngüsünün canlılığı. Worker'larda tek <c>Ready</c> etiketli kontrol bu
    /// olmalı: container'ın sağlıksız sayılması gereken tek durum, döngünün durmasıdır.
    ///
    /// <paramref name="staleAfter"/> tarama aralığının birkaç katı verilmeli — tek bir
    /// yavaş turda yanlış alarm üretmesin.
    /// </summary>
    public AppHealthChecksBuilder AddWorkerHeartbeat(TimeSpan staleAfter)
    {
        _builder.Services.AddSingleton<WorkerHeartbeat>();

        _builder.Add(new HealthCheckRegistration(
            name: "worker-loop",
            factory: sp => new WorkerHeartbeatHealthCheck(
                sp.GetRequiredService<WorkerHeartbeat>(), staleAfter),
            failureStatus: HealthStatus.Unhealthy,
            tags: [HealthCheckTags.Ready]));

        return this;
    }

    /// <summary>
    /// Ayar eksikse kontrolü atlamak yerine, doğrudan Unhealthy raporlayan bir yer
    /// tutucu kaydeder.
    ///
    /// Önce burada exception fırlatılıyordu; yanlıştı. Teşhis amaçlı bir özelliğin,
    /// izlemesi gereken çalışan servisi açılışta düşürmesi ters bir takas: yanlış
    /// yazılmış bir Redis adresi yüzünden, Redis olmadan da çalışabilecek servis hiç
    /// başlamıyordu. Şimdi servis ayağa kalkıyor, eksiklik panelde kırmızı görünüyor
    /// ve sebebi mesajda yazıyor.
    ///
    /// Sessizce atlamak da doğru değil: kontrol varmış gibi görünüp hiçbir şey
    /// doğrulamayan bir sağlık ucu bırakırdı. Yer tutucu ikisinin arasını buluyor.
    /// </summary>
    private bool Missing(string? value, string checkName, string configKey, string tag)
    {
        if (!string.IsNullOrWhiteSpace(value))
            return false;

        _builder.AddCheck(
            checkName,
            () => HealthCheckResult.Unhealthy($"Ayar eksik: '{configKey}'. Kontrol yapılandırılamadı."),
            tags: [tag]);

        return true;
    }
}
