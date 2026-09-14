using IdentityService.Application.Interfaces;

namespace IdentityService.WebApi.Infrastructure;

/// <summary>
/// Keycloak'ın güvenlik olaylarını periyodik olarak çekip kendi log hattımıza yazar.
///
/// NEDEN GEREKLİ: parola sıfırlama baştan sona Keycloak'ın içinde geçiyor —
/// istek, jeton, mail ve yeni parola formu hep orada. Servislerimiz bu akışı hiç
/// görmüyor, dolayısıyla "kim parolasını sıfırladı" sorusunun loglarımızda
/// cevabı yoktu. Denetim izinin ikiye bölünmemesi için olaylar buraya taşınıyor.
///
/// NEDEN YOKLAMA (polling): Keycloak olayları dışarı itmiyor; ya kendi
/// veritabanına yazıyor ya da kendi stdout'una. Gerçek zamanlı almanın yolu
/// Keycloak'a Java ile özel bir dinleyici (SPI) yazıp JAR olarak yüklemek —
/// bu kadar seyrek bir olay için orantısız. Güvenlik olayları nadir olduğundan
/// yarım dakikalık gecikme bir şey kaybettirmiyor.
///
/// KAYIT KAYBI İHTİMALİ: her yoklamada en yeni <see cref="MaxEventsPerPoll"/>
/// kayıt çekiliyor. İki yoklama arasında bundan fazla parola olayı olursa en
/// eskileri atlanır. Yalnızca parola olayları izlendiği için pratikte uzak bir
/// ihtimal; olsaydı bile Keycloak'ın kendi veritabanında duruyor olurlar.
/// </summary>
public sealed class KeycloakEventPoller : BackgroundService
{
    /// <summary>
    /// İzlenen olaylar. Tümü parola ile ilgili — giriş/çıkış olayları bilerek
    /// dışarıda: saniyede birkaç kez üretilip logları, tam da temizlediğimiz
    /// gürültüye geri döndürürlerdi.
    /// </summary>
    private static readonly string[] WatchedTypes =
    [
        "SEND_RESET_PASSWORD",       // sıfırlama maili gönderildi
        "SEND_RESET_PASSWORD_ERROR", // mail gönderilemedi
        "RESET_PASSWORD",            // kullanıcı parolasını sıfırladı
        "RESET_PASSWORD_ERROR",
        "UPDATE_PASSWORD",           // kullanıcı parolasını değiştirdi
        "UPDATE_PASSWORD_ERROR"
    ];

    private const int MaxEventsPerPoll = 100;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KeycloakEventPoller> _logger;
    private readonly TimeSpan _interval;

    /// <summary>
    /// En son işlenen olayın zamanı. Bellekte tutuluyor: servis yeniden
    /// başladığında sıfırlanır ve aşağıdaki ilk tur "geçmişi yazma" moduna
    /// girerek eski olayları tekrar loglamayı engeller.
    /// </summary>
    private long _lastSeenTime;

    private bool _firstPoll = true;

    public KeycloakEventPoller(
        IServiceScopeFactory scopeFactory,
        ILogger<KeycloakEventPoller> logger,
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
                await PollAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Keycloak erişilemezse döngü ölmemeli: bir sonraki turda yeniden
                // denenir. Uyarı seviyesinde, çünkü uygulamanın işleyişi
                // etkilenmiyor — yalnızca denetim kaydı gecikiyor.
                _logger.LogWarning(ex, "Keycloak güvenlik olayları okunamadı.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task PollAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var keycloak = scope.ServiceProvider.GetRequiredService<IKeycloakAdminClient>();

        var events = await keycloak.GetSecurityEventsAsync(
            WatchedTypes, MaxEventsPerPoll, cancellationToken);

        if (events.Count == 0)
            return;

        // İlk turda hiçbir şey loglanmıyor, yalnızca imleç en yeniye kuruluyor.
        // Aksi halde servis her yeniden başladığında Keycloak'ın elindeki tüm
        // geçmiş olaylar log'a yeniden düşerdi.
        if (_firstPoll)
        {
            _firstPoll = false;
            _lastSeenTime = events.Max(e => e.Time);
            return;
        }

        // Keycloak yeniden eskiye sıralı döndürüyor; eskiden yeniye yazmak için
        // ters çevriliyor, böylece log akışı gerçek sırayı yansıtıyor.
        var fresh = events
            .Where(e => e.Time > _lastSeenTime)
            .OrderBy(e => e.Time)
            .ToList();

        if (fresh.Count == 0)
            return;

        foreach (var securityEvent in fresh)
            Write(securityEvent);

        _lastSeenTime = fresh[^1].Time;
    }

    /// <summary>
    /// Olayı, diğer denetim satırlarıyla AYNI alan adlarıyla yazar: actor.id ve
    /// client.ip. Amaç, Kibana'da "bu kullanıcı neler yaptı" sorusunun tek bir
    /// filtreyle cevaplanabilmesi — parola olayları ayrı bir adlandırmada
    /// olsaydı o sorgunun dışında kalırlardı.
    /// </summary>
    private void Write(KeycloakSecurityEvent securityEvent)
    {
        var failed = securityEvent.Type.EndsWith("_ERROR", StringComparison.Ordinal);

        // Boş alanlar hiç yazılmıyor: eşleşen kullanıcı bulunamadığında Keycloak
        // userId göndermiyor ve "actor.id: null" satırı, aramayı kolaylaştırmak
        // yerine alanı kirletirdi.
        var fields = new Dictionary<string, object> { ["event.action"] = securityEvent.Type };

        if (!string.IsNullOrEmpty(securityEvent.UserId))
            fields["actor.id"] = securityEvent.UserId;

        if (!string.IsNullOrEmpty(securityEvent.IpAddress))
            fields["client.ip"] = securityEvent.IpAddress;

        using var _ = _logger.BeginScope(fields);

        // Başarısızlar uyarı: sıfırlama maili gönderilememesi ya da jetonun
        // reddedilmesi, kullanıcının takıldığı ve müdahale gerekebilecek bir
        // durum. Başarılılar bilgi seviyesinde, normal akışın parçası.
        if (failed)
        {
            _logger.LogWarning("Keycloak parola olayı başarısız: {EventType}", securityEvent.Type);
            return;
        }

        _logger.LogInformation("Keycloak parola olayı: {EventType}", securityEvent.Type);
    }
}
