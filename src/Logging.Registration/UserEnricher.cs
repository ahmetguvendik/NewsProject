using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Logging.Registration;

/// <summary>
/// Eylemi yapan kullanıcıyı her log satırına ekler.
///
/// Bu olmadan loglar "ne oldu" sorusunu cevaplıyor ama "kim yaptı" sorusunu
/// cevaplamıyordu. ECS formatter'ın yazdığı user.name alanı container'ın işletim
/// sistemi kullanıcısı (her zaman "root") — denetim açısından değersiz.
///
/// İSİMLENDİRME SİMETRİK:
///   actor.id        → eylemi YAPAN   (burada, token'dan)
///   target.user.id  → eylemin HEDEFİ (CommandLoggingBehavior, komuttan)
///
/// YALNIZCA KİMLİK YAZILIYOR. Ad/e-posta bilerek eklenmiyor: kişisel veri,
/// aranabilir bir depoda, her satırda tekrarlanır ve kullanıcı adresini
/// değiştirdiğinde eski kayıtlar yanıltıcı hale gelir. Ada ihtiyaç olduğunda
/// kimlikten veritabanına bakılır.
///
/// Roller de yazılmıyor: "o an admin miydi" sorusu, rol verme/alma işlemlerinin
/// kendisi komut olarak loglandığı için (AssignRoleCommand + target.role.name)
/// logların kendisinden kurulabiliyor — her satıra dizi damgalamaya gerek yok.
///
/// Aktör için ECS'in user.* alanı yerine actor.* seçildi. ECS'te user.* zaten
/// "eylemi yapan" demek, ama log'da user.id ile target.user.id yan yana
/// durduğunda hangisinin ne olduğu okunmuyordu. actor./target. çifti bu
/// belirsizliği tamamen kaldırıyor.
///
/// Bedeli: Elastic APM eklendiğinde kendi işlemleri için user.* yazacak, bizim
/// loglarımız actor.* taşıyacak. İkisini tek sorguda birleştirmek gerekirse
/// Elasticsearch'te alias tanımlamak yeterli — alan adını sonradan değiştirmeye
/// gerek kalmaz.
///
/// Worker'larda HTTP isteği olmadığı için sessizce atlanıyor. Bir yayının worker
/// tarafındaki adımlarını da kullanıcıya bağlamak isteniyorsa, kimliğin
/// traceparent gibi outbox satırında taşınması gerekir — şu an taşınmıyor.
/// </summary>
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

        // IP, kimlik kontrolünden ÖNCE yazılıyor. Sebebi: en çok ihtiyaç duyulan
        // satırlar anonim olanlar — başarısız giriş denemeleri, kayıt selleri,
        // hız sınırına takılan istekler. Bunların hiçbirinde token yok; kontrolden
        // sonra yazılsaydı tam da soruşturulması gereken trafik IP'siz kalırdı.
        var clientIp = ResolveClientIp(context);
        if (!string.IsNullOrEmpty(clientIp))
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("client.ip", clientIp));

        var user = context.User;

        if (user?.Identity?.IsAuthenticated != true)
            return;

        // Keycloak'ın "sub" claim'i — kullanıcının kalıcı kimliği. Denetim
        // aramalarının dayandığı alan bu.
        var keycloakId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(keycloakId))
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("actor.id", keycloakId));
    }

    /// <summary>
    /// İsteği yapanın gerçek IP'si.
    ///
    /// <c>RemoteIpAddress</c> TEK BAŞINA YETMİYOR: servisler APISIX'in arkasında
    /// duruyor, dolayısıyla o alan her istekte gateway container'ının IP'sini
    /// gösterir — tüm satırlar aynı adresi taşır ve alan işe yaramaz. Gerçek
    /// adres APISIX'in eklediği başlıkta geliyor.
    ///
    /// X-Forwarded-For bir zincir olabilir ("istemci, proxy1, proxy2"); ilk
    /// girdi en dıştaki istemcidir.
    ///
    /// GÜVENİLİRLİK SINIRI: bu başlıklar istemci tarafından uydurulabilir. Burada
    /// sorun değil çünkü değer yalnızca LOGLANIYOR, yetkilendirmede kullanılmıyor;
    /// ayrıca servis portları dışarı açılmadığı için istekler APISIX'ten geçmek
    /// zorunda ve APISIX başlığı kendi gördüğü adresle yeniden yazıyor. Bu alan
    /// ileride bir karara (engelleme, hız sınırı) dayanak yapılacaksa, o zaman
    /// yalnızca güvenilen proxy'lerden gelen başlığa itibar edilmeli.
    /// </summary>
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
