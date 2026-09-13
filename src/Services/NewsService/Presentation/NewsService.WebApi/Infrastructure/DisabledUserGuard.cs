using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Shared.Security;
using StackExchange.Redis;

namespace NewsService.WebApi.Infrastructure;

/// <summary>
/// Pasife alınmış bir kullanıcının admin işlemlerini reddeder.
///
/// ÇÖZDÜĞÜ SORUN: JWT geri alınamaz. Bir kullanıcıyı Keycloak'ta devre dışı
/// bırakmak yalnızca YENİ girişleri engelliyor; elindeki token süresi dolana
/// kadar geçerli kalıyor ve servisler her istekte yalnızca imzaya bakıyor.
/// Ölçüldüğünde durum şuydu: pasife alınan bir admin kendi token'ıyla
/// <c>/api/user/{kendi id}/activate</c> çağırıp kararı geri alabiliyor ve
/// yeniden giriş yapabiliyordu — yani pasife alma hiç işlemiyordu.
///
/// NEDEN BURADA, MediatR DAVRANIŞI OLARAK DEĞİL: davranış Application
/// katmanında durur ve HttpContext göremez; isteği yapanın kimliğini öğrenmesi
/// için ayrı bir soyutlama gerekirdi. Ayrıca MediaController MediatR
/// kullanmıyor, davranış oraya hiç uğramazdı. Middleware isteğin tamamını
/// görüyor ve yeni bir admin ucu yazıldığında kendiliğinden kapsıyor.
///
/// NEDEN PAYLAŞILAN BİR KÜTÜPHANEDE DEĞİL: bu kod tabanı web altyapısını servis
/// başına kopyalıyor (GlobalExceptionHandler, JwtErrorResponses,
/// KeycloakRolesClaimsTransformation — hepsi bu klasörde, her serviste ayrı).
/// IdentityService'te de bunun bir eşi var.
///
/// TOKEN ÖMRÜNE DOKUNMUYOR. Süreyi kısaltmak da bir seçenekti ama bu projede
/// istemci refresh_token kullanmıyor (bkz. src/Client/src/api/auth.ts): süre
/// dolduğunda kullanıcı doğrudan dışarı atılıyor. Ömrü beş dakikaya indirmek
/// herkesi beş dakikada bir oturumdan düşürürdü.
/// </summary>
public sealed class DisabledUserGuard
{
    private readonly RequestDelegate _next;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<DisabledUserGuard> _logger;

    public DisabledUserGuard(
        RequestDelegate next,
        IConnectionMultiplexer redis,
        ILogger<DisabledUserGuard> logger)
    {
        _next = next;
        _redis = redis;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequiresAdmin(context) || !await IsDisabledAsync(context))
        {
            await _next(context);
            return;
        }

        _logger.LogWarning("Pasife alınmış kullanıcı admin işlemi denedi: {Path}", context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json; charset=utf-8";

        await context.Response.WriteAsync(
            """
            {"status":403,"errorCode":"AUTH_FORBIDDEN","message":"Hesabınız pasife alınmış.","description":"Bu işlem için yetkiniz kaldırıldı. Devam etmek için bir yöneticiyle görüşün.","errors":null}
            """,
            context.RequestAborted);
    }

    /// <summary>
    /// Uç noktanın admin rolü isteyip istemediğini metadata'dan okur.
    ///
    /// Controller'lara elle bir işaret eklemek yerine mevcut
    /// <c>[Authorize(Roles = "admin")]</c> bildirimleri okunuyor: yeni bir admin
    /// ucu yazıldığında koruma kendiliğinden kapsıyor, kimsenin ikinci bir şeyi
    /// hatırlaması gerekmiyor.
    ///
    /// YALNIZCA ADMIN UÇLARI, bilinçli: editör yetkileri daraltıldıktan sonra
    /// pasif bir editörün yapabileceği tek şey kimsenin görmediği taslak alanına
    /// yazmak. Ağır sonuçlu işlemler (silme, yayınlama, rol atama, aktif/pasif
    /// etme) zaten admin rolünün arkasında.
    /// </summary>
    private static bool RequiresAdmin(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null) return false;

        foreach (var attribute in endpoint.Metadata.GetOrderedMetadata<AuthorizeAttribute>())
        {
            if (attribute.Roles is null) continue;

            foreach (var role in attribute.Roles.Split(','))
            {
                if (role.Trim().Equals("admin", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Kullanıcı pasif listesinde mi? Listeyi IdentityService yazıyor.
    ///
    /// AÇIK DAVRANIŞ (fail-open): Redis'e ulaşılamazsa istek REDDEDİLMİYOR.
    /// Kapalı davranış seçilseydi Redis'in kısa bir kesintisi tüm yönetim
    /// işlemlerini durdururdu. Projenin geri kalanı da aynı yönde kurulmuş:
    /// Redis erişilemediğinde uygulama çalışmaya devam ediyor. Kaçırılan senaryo
    /// dar — kesinti ANINDA, pasife alınmış ve token'ı hâlâ geçerli bir admin'in
    /// istek yapması. O durumda güvenlik bu kontrol eklenmeden önceki hâline
    /// dönüyor; yeni bir açık yaratmıyor, mevcudu kapatamıyor.
    /// </summary>
    private async Task<bool> IsDisabledAsync(HttpContext context)
    {
        var keycloakId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(keycloakId)) return false;

        if (!_redis.IsConnected)
        {
            _logger.LogWarning("Pasif kullanıcı listesi okunamadı (Redis bağlı değil); istek geçirildi.");
            return false;
        }

        try
        {
            return await _redis.GetDatabase().KeyExistsAsync(DisabledUsers.Key(keycloakId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Pasif kullanıcı listesi okunamadı; istek geçirildi.");
            return false;
        }
    }
}
