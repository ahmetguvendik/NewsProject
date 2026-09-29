using IdentityService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace IdentityService.Application.Auditing;

/// <summary>
/// Keycloak'ın güvenlik olaylarını okuyup kendi log hattımıza yazar.
///
/// NEDEN VAR: parola sıfırlama baştan sona Keycloak'ın içinde geçiyor — istek,
/// jeton, mail ve yeni parola formu hep orada. Servislerimiz bu akışı hiç
/// görmüyor, dolayısıyla loglarımızda da izi yoktu. Bu sınıf o boşluğu kapatıyor.
///
/// NEDEN Application'DA, BackgroundService'İN İÇİNDE DEĞİL: buradaki her karar
/// bir iş kuralı — hangi olay "yeni" sayılır, hangisi kaygı vericidir, denetim
/// satırı hangi alan adlarını taşır. Bunlar zamanlayıcıdan bağımsız ve tek
/// başlarına doğrulanabilir olmalı. Döngünün içine gömülüyken test etmenin tek
/// yolu servisi çalıştırıp Kibana'ya bakmaktı; denetim kaydının doğruluğu
/// (olay atlanmaması, çift yazılmaması) tam da test edilmesi gereken şey.
///
/// Ne BİLMEZ: ne sıklıkta çağrıldığını, <c>BackgroundService</c>'i, scope
/// yönetimini. Onlar barındıran tarafın işi (bkz. KeycloakEventPollerService).
/// Bu ayrım sayesinde iş, ilerde ayrı bir Worker'a taşınırsa bu dosyaya tek
/// satır dokunulmaz.
/// </summary>
public sealed class KeycloakEventSync
{
    /// <summary>
    /// İzlenen olay türleri. Yalnızca parola akışı: giriş/çıkış olayları
    /// saniyede birkaç kez üretilip hem Keycloak'ın tablosunu hem logları
    /// gürültüye boğardı.
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

    /// <summary>
    /// Tek turda okunacak azami kayıt. İki tur arasında bundan fazla olay
    /// olursa en eskileri atlanır — yoklama aralığıyla birlikte düşünülmeli.
    /// </summary>
    private const int MaxEventsPerPoll = 100;

    private readonly IKeycloakAdminClient _keycloak;
    private readonly IKeycloakEventCursor _cursor;
    private readonly ILogger<KeycloakEventSync> _logger;

    public KeycloakEventSync(
        IKeycloakAdminClient keycloak,
        IKeycloakEventCursor cursor,
        ILogger<KeycloakEventSync> logger)
    {
        _keycloak = keycloak;
        _cursor = cursor;
        _logger = logger;
    }

    /// <summary>
    /// Bir tur çalıştırır: yeni olayları okur, loglar, imleci ilerletir.
    /// Yazılan olay sayısını döndürür.
    /// </summary>
    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var events = await _keycloak.GetSecurityEventsAsync(
            WatchedTypes, MaxEventsPerPoll, cancellationToken);

        if (events.Count == 0)
            return 0;

        var lastSeen = await _cursor.ReadAsync(cancellationToken);

        // İmleç henüz kurulmamış: hiçbir şey loglanmıyor, yalnızca en yeniye
        // ayarlanıyor. Aksi halde imlecin sıfırlandığı her durumda Keycloak'ın
        // elindeki tüm geçmiş olaylar log'a yeniden düşerdi.
        if (lastSeen is null)
        {
            await _cursor.WriteAsync(events.Max(e => e.Time), cancellationToken);
            return 0;
        }

        // Keycloak yeniden eskiye sıralı döndürüyor; eskiden yeniye yazmak için
        // ters çevriliyor, böylece log akışı gerçek sırayı yansıtıyor.
        var fresh = events
            .Where(e => e.Time > lastSeen.Value)
            .OrderBy(e => e.Time)
            .ToList();

        if (fresh.Count == 0)
            return 0;

        foreach (var securityEvent in fresh)
            Write(securityEvent);

        await _cursor.WriteAsync(fresh[^1].Time, cancellationToken);
        return fresh.Count;
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
