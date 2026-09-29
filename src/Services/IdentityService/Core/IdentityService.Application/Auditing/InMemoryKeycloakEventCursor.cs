using IdentityService.Application.Interfaces;

namespace IdentityService.Application.Auditing;

/// <summary>
/// İmleci süreç belleğinde tutar.
///
/// SONUÇLARI — ikisi de bilinçli takas, ama bilinerek seçilmeli:
///
/// 1. SERVİS YENİDEN BAŞLARSA imleç sıfırlanır. Bir sonraki tur "geçmişi yazma"
///    moduna girer, yani yeniden başlatma SIRASINDA olan parola olayları hiç
///    loglanmaz. Alternatifi, Keycloak'ın elindeki tüm geçmişi her açılışta
///    log'a boşaltmaktı — denetim kaydını kullanılamaz hale getirirdi.
///
/// 2. TEK ÖRNEK VARSAYIYOR. identity-service iki replikaya çıkarsa her ikisi de
///    aynı olayları okur ve İKİSİ DE loglar; denetim kaydında çift satır oluşur.
///    Bugün docker-compose.yml'de replicas tanımı yok, dolayısıyla sorun değil.
///    Çoğaltmaya karar verildiğinde bu sınıfın yerine paylaşılan bir imleç
///    (Redis) konmalı — <see cref="IKeycloakEventCursor"/> tam da bunun için var.
///
/// Singleton olarak kaydedilmeli: imleç turlar arasında yaşamak zorunda.
/// </summary>
public sealed class InMemoryKeycloakEventCursor : IKeycloakEventCursor
{
    private long? _lastSeenTime;

    public ValueTask<long?> ReadAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(_lastSeenTime);

    public ValueTask WriteAsync(long time, CancellationToken cancellationToken = default)
    {
        _lastSeenTime = time;
        return ValueTask.CompletedTask;
    }
}
