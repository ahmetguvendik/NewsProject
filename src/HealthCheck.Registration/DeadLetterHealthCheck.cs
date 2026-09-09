using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheck.Registration;

/// <summary>
/// Dead-letter'a düşmüş mesaj sayısını raporlar.
///
/// Bu kontrol olmadan bir mesajın ölmesi TAMAMEN görünmezdi: worker sağlıklı,
/// container ayakta, log'da bir satır — ve mail hiç gitmiyor. Kullanıcı şikayet
/// edene kadar kimsenin haberi olmuyordu.
///
/// SAYIM, LOG DEĞİL: "kaç mesaj ölü" bir durum sorusu, log'da aranacak bir desen
/// değil. Bu yüzden uyarıyı log altyapısına değil sağlık kontrolüne bağladık.
/// </summary>
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
            // Sayım yapılamadıysa "sorun yok" demek yanlış olur — belirsizlik de
            // görünür olmalı, sessizce yeşil kalmamalı.
            return HealthCheckResult.Unhealthy("Dead-letter sayısı okunamadı.", ex);
        }
    }
}
