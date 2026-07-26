using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Extensions;

/// <summary>
/// Uygulama açılışında EF Core migration'larını uygular.
/// Container ortamında servis, Postgres hazır olmadan ayağa kalkabildiği için
/// bağlantı hataları belirli sayıda yeniden denenir.
/// </summary>
public static class HostMigrationExtensions
{
    public static async Task<IHost> MigrateDatabaseAsync<TContext>(
        this IHost host,
        int maxAttempts = 12,
        int delaySeconds = 5,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        using var scope = host.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TContext>>();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await context.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("{Context} migration'ları uygulandı.", typeof(TContext).Name);
                return host;
            }
            catch (Exception ex) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    "{Context} migration denemesi {Attempt}/{Max} başarısız ({Error}). {Delay} sn sonra tekrar denenecek.",
                    typeof(TContext).Name, attempt, maxAttempts, ex.Message, delaySeconds);

                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
            }
        }

        // Son deneme: hata yutulmaz, container fail eder ve restart policy devreye girer.
        await context.Database.MigrateAsync(cancellationToken);
        return host;
    }
}
