using IdentityService.Application.Auditing;

namespace IdentityService.WebApi.BackgroundServices;

public sealed class KeycloakEventPollerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KeycloakEventPollerService> _logger;
    private readonly TimeSpan _interval;

    public KeycloakEventPollerService(
        IServiceScopeFactory scopeFactory,
        ILogger<KeycloakEventPollerService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = TimeSpan.FromSeconds(
            configuration.GetValue("Keycloak:EventPollSeconds", 30));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sync = scope.ServiceProvider.GetRequiredService<KeycloakEventSync>();

                await sync.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Kapanış — hata değil.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Keycloak güvenlik olayları okunamadı.");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
