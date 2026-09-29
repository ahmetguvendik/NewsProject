using IdentityService.Application.Auditing;

namespace IdentityService.WebApi.BackgroundServices;

/// <summary>
/// <see cref="KeycloakEventSync"/>'i belirli aralıklarla çalıştıran tetikleyici.
///
/// SADECE TETİKLEYİCİ. Hangi olay yeni sayılır, hangisi uyarı üretir, denetim
/// satırı nasıl görünür — hiçbiri burada değil, hepsi Application katmanında.
/// Buradaki tek karar "ne sıklıkta" ve "hata olursa ne yapılır".
///
/// Bir HTTP isteği nasıl uygulamayı tetikleyen bir giriş noktasıysa, zamanlayıcı
/// da öyle. O yüzden Presentation'da durması doğru — ama Infrastructure
/// klasöründe değil: oradaki her şey (GlobalExceptionHandler, JwtErrorResponses,
/// RevokedTokenGuard, KeycloakRolesClaimsTransformation) HTTP hattına bağlı,
/// bu ise HttpContext'i hiç görmüyor.
///
/// TAŞINABİLİR: identity-service çoğaltılmaya karar verildiğinde bu sınıf
/// src/Workers/ altına bir Worker olarak kopyalanır ve Application'daki iş
/// mantığına dokunulmaz. O gün imlecin de paylaşılan bir kaynağa taşınması
/// gerekir (bkz. InMemoryKeycloakEventCursor).
/// </summary>
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
                // Her turda yeni scope: KeycloakEventSync scoped kayıtlı, bu
                // sınıf ise singleton — doğrudan enjekte edilemez.
                //
                // Scoped olmasının asıl sebebi taşıdığı IKeycloakAdminClient:
                // IHttpClientFactory'den gelen bir typed HttpClient. Uzun ömürlü
                // bir nesnede tutulsaydı tek bir HttpMessageHandler'a kilitlenir,
                // fabrikanın handler yenilemesi (DNS değişikliklerini görmesi)
                // devre dışı kalırdı. Her tur taze bir istemci alıyor.
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
                // Keycloak erişilemezse döngü ölmemeli: bir sonraki turda yeniden
                // denenir. Uyarı seviyesinde, çünkü uygulamanın işleyişi
                // etkilenmiyor — yalnızca denetim kaydı gecikiyor.
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
