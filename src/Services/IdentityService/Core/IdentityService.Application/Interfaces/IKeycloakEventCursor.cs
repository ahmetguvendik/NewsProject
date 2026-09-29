namespace IdentityService.Application.Interfaces;

/// <summary>
/// Keycloak olaylarında nereye kadar geldiğimizi tutar.
///
/// NEDEN AYRI BİR SÖZLEŞME: imleç, senkronizasyonun DOĞRULUĞUNU belirleyen tek
/// şey — nerede tutulduğu ise bir dağıtım kararı. Bugün bellekte (tek örnek
/// varsayımıyla), yarın Redis'te olabilir. Arayüz arkasında durduğu için o geçiş
/// yeni bir sınıf ekleyip DI kaydını değiştirmekten ibaret kalıyor;
/// <see cref="Auditing.KeycloakEventSync"/> hiç değişmiyor.
///
/// Zaman değerleri Keycloak'ın verdiği biçimde: Unix epoch, milisaniye.
/// </summary>
public interface IKeycloakEventCursor
{
    /// <summary>
    /// İmleç daha hiç kurulmadıysa <c>null</c>. Çağıran taraf bunu "geçmişi
    /// yazma, yalnızca imleci kur" olarak yorumluyor.
    /// </summary>
    ValueTask<long?> ReadAsync(CancellationToken cancellationToken = default);

    ValueTask WriteAsync(long time, CancellationToken cancellationToken = default);
}
