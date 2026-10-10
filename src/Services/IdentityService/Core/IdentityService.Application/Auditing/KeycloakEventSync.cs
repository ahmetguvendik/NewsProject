using IdentityService.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace IdentityService.Application.Auditing;

public sealed class KeycloakEventSync
{
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

    private readonly IKeycloakAdminClient _keycloak;
    private readonly IKeycloakEventCursor _cursor;
    private readonly TimeProvider _clock;
    private readonly ILogger<KeycloakEventSync> _logger;

    public KeycloakEventSync(
        IKeycloakAdminClient keycloak,
        IKeycloakEventCursor cursor,
        TimeProvider clock,
        ILogger<KeycloakEventSync> logger)
    {
        _keycloak = keycloak;
        _cursor = cursor;
        _clock = clock;
        _logger = logger;
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken = default)
    {
        var lastSeen = await _cursor.ReadAsync(cancellationToken);

        // İlk tur: imleci şu ana kur, öncesini geçmiş say.
        if (lastSeen is null)
        {
            await _cursor.WriteAsync(_clock.GetUtcNow().ToUnixTimeMilliseconds(), cancellationToken);
            return 0;
        }

        var events = await _keycloak.GetSecurityEventsAsync(
            WatchedTypes, MaxEventsPerPoll, cancellationToken);

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

    private void Write(KeycloakSecurityEvent securityEvent)
    {
        var failed = securityEvent.Type.EndsWith("_ERROR", StringComparison.Ordinal);

        var fields = new Dictionary<string, object> { ["event.action"] = securityEvent.Type };

        if (!string.IsNullOrEmpty(securityEvent.UserId))
            fields["actor.id"] = securityEvent.UserId;

        if (!string.IsNullOrEmpty(securityEvent.IpAddress))
            fields["client.ip"] = securityEvent.IpAddress;

        using var _ = _logger.BeginScope(fields);

        if (failed)
        {
            _logger.LogWarning("Keycloak parola olayı başarısız: {EventType}", securityEvent.Type);
            return;
        }

        _logger.LogInformation("Keycloak parola olayı: {EventType}", securityEvent.Type);
    }
}
