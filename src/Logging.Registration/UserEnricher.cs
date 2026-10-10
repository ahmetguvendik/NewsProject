using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Logging.Registration;

// Yalnızca kimlik yazılır; ad/e-posta kişisel veri olduğu için log'a girmez.
public sealed class UserEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null)
            return;

        // Kimlik kontrolünden önce: IP'ye en çok anonim istekler (başarısız giriş, hız sınırı) için ihtiyaç var.
        var clientIp = ResolveClientIp(context);
        if (!string.IsNullOrEmpty(clientIp))
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("client.ip", clientIp));

        var user = context.User;

        if (user?.Identity?.IsAuthenticated != true)
            return;

        var keycloakId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(keycloakId))
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("actor.id", keycloakId));
    }

    // APISIX arkasında RemoteIpAddress hep gateway'i gösterir; gerçek adres başlıkta.
    // Başlık uydurulabilir: yalnızca loglama için, yetki kararında kullanılmamalı.
    private static string? ResolveClientIp(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();

        if (!string.IsNullOrWhiteSpace(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        var realIp = context.Request.Headers["X-Real-IP"].ToString();

        if (!string.IsNullOrWhiteSpace(realIp))
            return realIp.Trim();

        return context.Connection.RemoteIpAddress?.ToString();
    }
}
