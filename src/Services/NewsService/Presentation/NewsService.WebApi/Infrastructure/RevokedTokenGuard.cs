using System.Security.Claims;
using System.Text.Json;
using Shared.Exceptions;
using Shared.Security;
using StackExchange.Redis;

namespace NewsService.WebApi.Infrastructure;

public sealed class RevokedTokenGuard
{
    private static readonly string DisabledBody =
        $$"""
        {"status":403,"errorCode":"{{ErrorCodes.Auth.AccountDisabled}}","message":"Hesabınız pasife alınmış.","description":"Bu işlem için yetkiniz kaldırıldı. Devam etmek için bir yöneticiyle görüşün.","errors":null}
        """;

    private static readonly string RolesBody =
        $$"""
        {"status":401,"errorCode":"{{ErrorCodes.Auth.TokenStale}}","message":"Yetkileriniz değişti.","description":"Oturumunuz güncel değil. Lütfen tekrar giriş yapın.","errors":null}
        """;

    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RevokedTokenGuard> _logger;

    public RevokedTokenGuard(
        RequestDelegate next,
        IConnectionMultiplexer redis,
        ILogger<RevokedTokenGuard> logger)
    {
        _next = next;
        _redis = redis;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var reason = await RevocationReasonAsync(context);
        if (reason is null)
        {
            await _next(context);
            return;
        }

        var (status, body) = reason switch
        {
            TokenRevocation.ReasonAccountDisabled => (StatusCodes.Status403Forbidden, DisabledBody),
            _ => (StatusCodes.Status401Unauthorized, RolesBody)
        };

        _logger.LogWarning("Token ile gelen istek reddedildi [{Reason}]: {Path}", reason, context.Request.Path);

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json; charset=utf-8";

        await context.Response.WriteAsync(body, context.RequestAborted);
    }

    private async Task<string?> RevocationReasonAsync(HttpContext context)
    {
        var keycloakId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(keycloakId)) return null;

        if (!_redis.IsConnected) return Unreadable(null);

        string? raw;
        try
        {
            raw = await _redis.GetDatabase().StringGetAsync(TokenRevocation.Key(keycloakId));
        }
        catch (Exception ex)
        {
            return Unreadable(ex);
        }

        var revocation = TokenRevocation.Parse(raw);
        if (revocation is null) return null;

        var issuedAt = IssuedAtUnix(context);
        if (issuedAt is null)
        {
            _logger.LogError("Token'ın iat değeri okunamadı; istek reddedildi. KullanıcıId={KeycloakId}", keycloakId);
            return revocation.Reason;
        }

        // Kayıttan SONRA üretilmiş token geçerli: kullanıcı tekrar giriş yapmış.
        return revocation.Covers(issuedAt.Value) ? revocation.Reason : null;
    }

    // Kayıt okunamazsa istek geçer: Redis kesintisi sistemi durdurmasın. İptal edilmiş token
    // bu sürede, kendi süresi (en fazla 1 saat) dolana kadar geçerli kalır.
    private string? Unreadable(Exception? ex)
    {
        _logger.LogWarning(ex, "İptal kaydı okunamadı; istek geçirildi.");
        return null;
    }

    private static long? IssuedAtUnix(HttpContext context)
    {
        if (long.TryParse(context.User.FindFirstValue("iat"), out var fromClaim))
            return fromClaim;

        const string prefix = "Bearer ";
        var header = context.Request.Headers.Authorization.ToString();
        if (!header.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return null;

        var segments = header[prefix.Length..].Trim().Split('.');
        if (segments.Length < 2) return null;

        try
        {
            var payload = segments[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            return document.RootElement.TryGetProperty("iat", out var iat) && iat.TryGetInt64(out var seconds)
                ? seconds
                : null;
        }
        catch
        {
            return null;
        }
    }
}
