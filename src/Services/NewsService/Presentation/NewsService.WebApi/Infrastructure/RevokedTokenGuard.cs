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
        {"status":403,"errorCode":"{{ErrorCodes.Auth.Forbidden}}","message":"Hesabınız pasife alınmış.","description":"Bu işlem için yetkiniz kaldırıldı. Devam etmek için bir yöneticiyle görüşün.","errors":null}
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

        var disabled = reason == TokenRevocation.ReasonAccountDisabled;

        _logger.LogWarning(
            "Geçersiz kılınmış token ile istek reddedildi [{Reason}]: {Path}",
            reason, context.Request.Path);

        context.Response.StatusCode = disabled
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json; charset=utf-8";

        await context.Response.WriteAsync(disabled ? DisabledBody : RolesBody, context.RequestAborted);
    }

    private async Task<string?> RevocationReasonAsync(HttpContext context)
    {
        var keycloakId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(keycloakId)) return null;

        if (!_redis.IsConnected)
        {
            _logger.LogWarning("İptal kaydı okunamadı (Redis bağlı değil); istek geçirildi.");
            return null;
        }

        string? raw;
        try
        {
            raw = await _redis.GetDatabase().StringGetAsync(TokenRevocation.Key(keycloakId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "İptal kaydı okunamadı; istek geçirildi.");
            return null;
        }

        var revocation = TokenRevocation.Parse(raw);
        if (revocation is null) return null;

        var issuedAt = IssuedAtUnix(context);
        if (issuedAt is null)
        {
            _logger.LogError(
                "Token'ın iat değeri okunamadı; geçersiz kılma kontrolü atlandı. KullanıcıId={KeycloakId}",
                keycloakId);
            return null;
        }

        // Kayıttan SONRA üretilmiş token geçerli: kullanıcı tekrar giriş yapmış.
        return revocation.Covers(issuedAt.Value) ? revocation.Reason : null;
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
