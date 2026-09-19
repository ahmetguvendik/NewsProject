using System.Security.Claims;
using System.Text.Json;
using Shared.Security;
using StackExchange.Redis;

namespace NewsService.WebApi.Infrastructure;

/// <summary>
/// Geçersiz kılınmış token'larla gelen istekleri reddeder.
///
/// ÇÖZDÜĞÜ SORUN: JWT geri alınamaz. Kullanıcı pasife alındığında ya da rolü
/// değiştiğinde elindeki token süresi dolana kadar (1 saat) hiçbir şey olmamış
/// gibi çalışıyor. Gerekçenin tamamı <see cref="TokenInvalidation"/> içinde.
///
/// NEDEN MIDDLEWARE, MediatR DAVRANIŞI DEĞİL: davranış Application katmanında
/// durur ve HttpContext göremez; isteği yapanın kimliğini öğrenmesi için ayrı
/// bir soyutlama gerekirdi. Ayrıca MediaController MediatR kullanmıyor, davranış
/// oraya hiç uğramazdı. Middleware isteğin tamamını görüyor ve yeni bir uç
/// yazıldığında kendiliğinden kapsıyor.
///
/// NEDEN AYRI BİR DOĞRULAMA UCU DEĞİL: her istekte IdentityService'e HTTP turu
/// demek olurdu — bu servisin sıcak yolu başka bir servisin ayakta olmasına
/// bağlanırdı. Redis zaten ortak bağımlılık ve anahtar biçimi Shared'da.
///
/// NEDEN HER İSTEKTE, YALNIZCA ADMIN UÇLARINDA DEĞİL: eski hâli yalnızca
/// <c>[Authorize(Roles = "admin")]</c> taşıyan uçlara bakıyordu, çünkü tek
/// derdi pasife alınmış admin'di. Damga mekanizmasıyla rolü İNDİRİLEN kullanıcı
/// da yakalanmak isteniyor; kimliği olan her istek kontrol ediliyor. Kimliksiz
/// (anonim) isteklerde hiçbir iş yapılmıyor, okuma trafiğine maliyeti sıfır.
///
/// NEDEN UseAuthorization'DAN SONRA: buradaki kontrol yetkilendirmenin yerine
/// geçmiyor, ona ek. Sıra bu olunca context.User dolu geliyor.
///
/// TOKEN ÖMRÜNE DOKUNMUYOR. Süreyi kısaltmak da bir seçenekti ama bu projede
/// istemci refresh_token kullanmıyor (bkz. src/Client/src/api/auth.ts): süre
/// dolduğunda kullanıcı doğrudan dışarı atılıyor. Ömrü beş dakikaya indirmek
/// herkesi beş dakikada bir oturumdan düşürürdü.
/// </summary>
public sealed class RevokedTokenGuard
{
    private const string DisabledBody =
        """
        {"status":403,"errorCode":"AUTH_FORBIDDEN","message":"Hesabınız pasife alınmış.","description":"Bu işlem için yetkiniz kaldırıldı. Devam etmek için bir yöneticiyle görüşün.","errors":null}
        """;

    // 401: istemci bunu görünce token'ı siliyor ve giriş ekranına yönlendiriyor
    // (bkz. src/Client/src/api/http.ts). Rol değişiminde doğru davranış bu —
    // tekrar giriş yapmak sorunu çözüyor, 403 ise çıkmaz sokak gibi okunurdu.
    private const string RolesBody =
        """
        {"status":401,"errorCode":"AUTH_TOKEN_STALE","message":"Yetkileriniz değişti.","description":"Oturumunuz güncel değil. Lütfen tekrar giriş yapın.","errors":null}
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

        var disabled = reason == TokenInvalidation.ReasonDisabled;

        _logger.LogWarning(
            "Geçersiz kılınmış token ile istek reddedildi [{Reason}]: {Path}",
            reason, context.Request.Path);

        context.Response.StatusCode = disabled
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json; charset=utf-8";

        await context.Response.WriteAsync(disabled ? DisabledBody : RolesBody, context.RequestAborted);
    }

    /// <summary>
    /// İstek geçersiz kılınmış bir token taşıyorsa gerekçesini, taşımıyorsa
    /// <c>null</c> döner.
    ///
    /// AÇIK DAVRANIŞ (fail-open): Redis'e ulaşılamazsa istek REDDEDİLMİYOR.
    /// Kapalı davranış seçilseydi Redis'in kısa bir kesintisi bütün siteyi
    /// durdururdu — artık yalnızca admin uçları değil her istek buradan geçiyor.
    /// Projenin geri kalanı da aynı yönde kurulmuş. Kaçırılan senaryo dar:
    /// kesinti ANINDA, yetkisi değişmiş ve token'ı hâlâ geçerli bir kullanıcının
    /// istek yapması. O durumda güvenlik bu kontrol eklenmeden önceki hâline
    /// dönüyor; yeni bir açık yaratmıyor, mevcudu kapatamıyor.
    /// </summary>
    private async Task<string?> RevocationReasonAsync(HttpContext context)
    {
        var keycloakId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(keycloakId)) return null;

        if (!_redis.IsConnected)
        {
            _logger.LogWarning("Geçersiz token listesi okunamadı (Redis bağlı değil); istek geçirildi.");
            return null;
        }

        string? stamp;
        try
        {
            stamp = await _redis.GetDatabase().StringGetAsync(TokenInvalidation.Key(keycloakId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Geçersiz token listesi okunamadı; istek geçirildi.");
            return null;
        }

        if (!TokenInvalidation.TryParse(stamp, out var invalidBefore, out var reason))
            return null;

        var issuedAt = IssuedAtUnix(context);
        if (issuedAt is null)
        {
            // Keycloak token'ları her zaman iat taşır; buraya düşmek için hem
            // claim'in hem de ham gövdenin okunamaması gerekir. Sessiz kalırsa
            // kontrol kendini kapatmış olur, o yüzden Error seviyesinde.
            _logger.LogError(
                "Token'ın iat değeri okunamadı; geçersiz kılma kontrolü atlandı. KullanıcıId={KeycloakId}",
                keycloakId);
            return null;
        }

        // Damgadan SONRA üretilmiş token geçerli: kullanıcı tekrar giriş yapmış.
        return issuedAt.Value <= invalidBefore ? reason : null;
    }

    /// <summary>
    /// Token'ın üretilme anı (<c>iat</c>, Unix saniye).
    ///
    /// Önce claim'e bakılıyor. JwtBearer bazı standart claim'leri Microsoft
    /// şemasına çeviriyor ve bu eşlemenin içeriği sürüme bağlı; bir güvenlik
    /// kontrolünü ona dayandırmamak için claim bulunamazsa token'ın gövdesi
    /// doğrudan okunuyor. Burada yapılan iş imza doğrulamak DEĞİL — token bu
    /// noktada JwtBearer tarafından çoktan doğrulanmış durumda, tek bir alan
    /// okunuyor.
    /// </summary>
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
