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
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity?.IsAuthenticated != true)
            return;

        // Keycloak'ın "sub" claim'i — kullanıcının kalıcı kimliği. Denetim
        // aramalarının dayandığı alan bu.
        var keycloakId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrEmpty(keycloakId))
            logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty("actor.id", keycloakId));
    }
}
