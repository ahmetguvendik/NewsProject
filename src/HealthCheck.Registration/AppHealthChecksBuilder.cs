using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheck.Registration;

public sealed class AppHealthChecksBuilder
{
    // Zorunlu: timeout'suz kontrol süresiz bekler; Kafka durunca /health hiç cevap vermemişti.
    private static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(5);

    private readonly IHealthChecksBuilder _builder;
    private readonly IConfiguration _configuration;

    internal AppHealthChecksBuilder(IHealthChecksBuilder builder, IConfiguration configuration)
    {
        _builder = builder;
        _configuration = configuration;
    }

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

    public AppHealthChecksBuilder AddKafka(string configKey = "Kafka:BootstrapServers")
    {
        var bootstrapServers = _configuration[configKey];
        if (Missing(bootstrapServers, "kafka", configKey, HealthCheckTags.Dependency))
            return this;

        _builder.AddKafka(
            new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                // Kafka kapalıyken kontrol librdkafka'nın uzun varsayılanlarında takılmasın.
                MessageTimeoutMs = 3000,
                SocketTimeoutMs = 3000
            },
            name: "kafka",
            failureStatus: HealthStatus.Unhealthy,
            tags: [HealthCheckTags.Dependency],
            timeout: CheckTimeout);

        return this;
    }

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

    // Ready değil Dependency: tek bozuk mesaj yüzünden çalışan worker durdurulmasın.
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

    // Ayar eksikse kontrol sessizce atlanmaz, panelde kırmızı görünür.
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
