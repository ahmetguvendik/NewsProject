using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheck.Registration;

/// <summary>
/// Worker döngüsünün her turda dokunduğu zaman damgası.
///
/// Worker'larda "hazır mı" sorusu anlamsız — kimse onlara trafik yönlendirmiyor.
/// Asıl merak edilen şey farklı: <b>döngü hâlâ dönüyor mu?</b> Çöken bir process'i
/// <c>restart: always</c> zaten toparlıyor, ama veritabanı çağrısında asılı kalan
/// bir döngü sonsuza kadar sessizce durur ve hiçbir şey fark etmez.
/// </summary>
public sealed class WorkerHeartbeat
{
    private long _lastBeatTicks = DateTime.UtcNow.Ticks;

    public void Beat() => Interlocked.Exchange(ref _lastBeatTicks, DateTime.UtcNow.Ticks);

    public DateTime LastBeatUtc => new(Interlocked.Read(ref _lastBeatTicks), DateTimeKind.Utc);
}

internal sealed class WorkerHeartbeatHealthCheck : IHealthCheck
{
    private readonly WorkerHeartbeat _heartbeat;
    private readonly TimeSpan _staleAfter;

    public WorkerHeartbeatHealthCheck(WorkerHeartbeat heartbeat, TimeSpan staleAfter)
    {
        _heartbeat = heartbeat;
        _staleAfter = staleAfter;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var age = DateTime.UtcNow - _heartbeat.LastBeatUtc;

        var result = age > _staleAfter
            ? HealthCheckResult.Unhealthy(
                $"Worker döngüsü {age.TotalSeconds:F0} sn'dir tur atmadı (eşik {_staleAfter.TotalSeconds:F0} sn).")
            : HealthCheckResult.Healthy($"Son tur {age.TotalSeconds:F0} sn önce.");

        return Task.FromResult(result);
    }
}
