using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheck.Registration;

internal sealed class DeadLetterHealthCheck : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Func<IServiceProvider, CancellationToken, Task<int>> _countAsync;

    public DeadLetterHealthCheck(
        IServiceScopeFactory scopeFactory,
        Func<IServiceProvider, CancellationToken, Task<int>> countAsync)
    {
        _scopeFactory = scopeFactory;
        _countAsync = countAsync;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var count = await _countAsync(scope.ServiceProvider, cancellationToken);

            return count == 0
                ? HealthCheckResult.Healthy("Dead-letter kuyruğu boş.")
                : HealthCheckResult.Unhealthy(
                    $"{count} mesaj dead-letter'da; işlenemedikleri için elle incelenmeleri gerekiyor.");
        }
        catch (Exception ex)
        {
            // Sayım yapılamadıysa "sorun yok" denmez: belirsizlik de görünür olmalı.
            return HealthCheckResult.Unhealthy("Dead-letter sayısı okunamadı.", ex);
        }
    }
}
