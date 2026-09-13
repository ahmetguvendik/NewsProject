namespace Shared.Security;

/// <summary>
/// Pasife alınmış kullanıcıların paylaşılan listesi için anahtar sözleşmesi.
///
/// NEDEN VAR: JWT geri alınamaz. Bir kullanıcı pasife alındığında Keycloak
/// yalnızca YENİ girişleri engelliyor; elindeki token süresi dolana kadar
/// geçerli kalıyor ve servisler her istekte yalnızca imzaya bakıyor. Bunun
/// somut sonucu şuydu: pasife alınan bir admin kendi token'ıyla
/// <c>/api/user/{kendi id}/activate</c> çağırıp kararı geri alabiliyordu —
/// yani pasife alma hiç işlemiyordu.
///
/// ÇÖZÜM: IdentityService pasife aldığı kullanıcıyı buraya yazıyor, admin
/// yetkisi isteyen uçlar isteği karşılamadan önce buraya bakıyor.
///
/// NEDEN Shared'DA: bu bir servis sınırını geçen sözleşme. Yazan IdentityService,
/// okuyan NewsService ve IdentityService'in kendisi. Anahtar biçimi iki tarafta
/// ayrı ayrı yazılsaydı, birinde yapılan bir değişiklik diğerini sessizce kör
/// bırakırdı — kontrol hiçbir zaman eşleşmez, hata da vermezdi.
/// </summary>
public static class DisabledUsers
{
    /// <summary>
    /// Verilen Keycloak kimliği için liste anahtarı.
    ///
    /// Anahtarın VARLIĞI "bu kullanıcı pasif" demek; değeri taşımıyor. Yokluğu
    /// "sorun yok" demek — yani normal akışta ek bir anlam çıkarımı gerekmiyor.
    /// </summary>
    public static string Key(string keycloakId) => $"identity:disabled:{keycloakId}";

    /// <summary>
    /// Kaydın listede kalma süresi.
    ///
    /// Kalıcı olmasına gerek YOK: bir kaydın tek işi, pasife alma anında hayatta
    /// olan token'ların ömrünü doldurmasını beklemek. O süre geçtikten sonra
    /// token zaten geçersiz ve kullanıcı giriş de yapamıyor (Keycloak'ta devre
    /// dışı). Bu yüzden TTL, erişim token'ının azami ömründen biraz uzun
    /// tutuluyor ve liste kendiliğinden temizleniyor — hiçbir zaman birkaç
    /// kayıttan fazla büyümüyor.
    ///
    /// Realm'deki accessTokenLifespan şu an 3600 sn. O değer DÜŞÜRÜLÜRSE burası
    /// olduğu gibi kalabilir (fazladan beklemek zararsız); YÜKSELTİLİRSE burası
    /// da yükseltilmeli, yoksa arada kontrolsüz bir pencere açılır.
    /// </summary>
    public static readonly TimeSpan Retention = TimeSpan.FromHours(2);
}
