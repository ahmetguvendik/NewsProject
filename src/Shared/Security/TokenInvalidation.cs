namespace Shared.Security;

/// <summary>
/// Geçersiz kılınmış token'ların paylaşılan listesi için anahtar sözleşmesi.
///
/// NEDEN VAR: JWT geri alınamaz. Token kendi kendine yeterli — servisler her
/// istekte yalnızca imzaya ve süreye bakıyor, kullanıcı hakkında sonradan olan
/// hiçbir şeyi görmüyorlar. Ölçüldüğünde iki somut sonucu vardı:
///
///   1. Pasife alınan bir admin kendi token'ıyla <c>/api/user/{kendi id}/activate</c>
///      çağırıp kararı geri alabiliyordu.
///   2. Admin rolü KALDIRILAN bir kullanıcı, elindeki token'la
///      <c>POST /api/user/roles</c> çağırıp kendine admin rolünü geri
///      verebiliyordu. Pencere, erişim token'ının ömrü kadar: 1 saat.
///
/// İkincisi birincinin guard'ına takılmıyordu: kullanıcı "pasif" değil, sadece
/// rütbesi indirilmiş — pasif listesinde adı geçmiyor.
///
/// ÇÖZÜM: kullanıcı hakkında yetki etkileyen bir şey değiştiğinde IdentityService
/// buraya bir DAMGA yazıyor: "bu andan önce üretilmiş token'lar artık geçersiz".
/// Servisler her istekte token'ın <c>iat</c> değerini bu damgayla karşılaştırıyor.
///
/// NEDEN ZAMAN DAMGASI, "VAR/YOK" DEĞİL: kullanıcı tekrar giriş yaptığında yeni
/// token'ın iat'i damgadan büyük olur ve kendiliğinden geçerli sayılır. Var/yok
/// olsaydı, rol değişen kullanıcı TTL dolana kadar giriş yapsa bile içeri
/// alınamazdı.
///
/// NEDEN Keycloak'A SORMUYORUZ: introspection "oturum yaşıyor mu" diye cevap
/// veriyor. Rol kaldırmak oturumu öldürmediği için tam da yakalamak istediğimiz
/// vakada <c>active: true</c> döner. Üstelik her istekte ağ turu demek ve
/// Keycloak'ı sıcak yola zorunlu bağımlılık haline getirirdi.
///
/// NEDEN Shared'DA: bu bir servis sınırını geçen sözleşme. Yazan IdentityService,
/// okuyan NewsService ve IdentityService'in kendisi. Anahtar biçimi iki tarafta
/// ayrı ayrı yazılsaydı, birinde yapılan bir değişiklik diğerini sessizce kör
/// bırakırdı — kontrol hiçbir zaman eşleşmez, hata da vermezdi.
/// </summary>
public static class TokenInvalidation
{
    /// <summary>Verilen Keycloak kimliği için damga anahtarı.</summary>
    public static string Key(string keycloakId) => $"identity:invalidate-before:{keycloakId}";

    /// <summary>
    /// Damganın listede kalma süresi.
    ///
    /// Kalıcı olmasına gerek YOK: bir damganın tek işi, yazıldığı anda hayatta
    /// olan token'ların ömrünü doldurmasını beklemek. O süre geçtikten sonra
    /// zaten hiçbir eski token geçerli değil.
    ///
    /// Realm'deki accessTokenLifespan şu an 3600 sn. O değer DÜŞÜRÜLÜRSE burası
    /// olduğu gibi kalabilir (fazladan beklemek zararsız); YÜKSELTİLİRSE burası
    /// da yükseltilmeli, yoksa arada kontrolsüz bir pencere açılır.
    /// </summary>
    public static readonly TimeSpan Retention = TimeSpan.FromHours(2);

    /// <summary>Hesap pasife alındı. Tekrar giriş çözmez — Keycloak da engelliyor.</summary>
    public const string ReasonDisabled = "disabled";

    /// <summary>Rol atandı veya kaldırıldı. Tekrar giriş yapmak çözer.</summary>
    public const string ReasonRoles = "roles";

    /// <summary>
    /// Damganın değeri: <c>&lt;unix saniye&gt;|&lt;gerekçe&gt;</c>.
    ///
    /// JSON yerine bu biçim seçildi çünkü değer redis-cli'de çıplak gözle
    /// okunabiliyor ve iki tarafta da ayrıştırmak tek <c>Split</c>.
    /// </summary>
    public static string Value(DateTimeOffset at, string reason) =>
        $"{at.ToUnixTimeSeconds()}|{reason}";

    /// <summary>
    /// Damgayı çözer. Biçim tanınmazsa <c>false</c> döner — çağıran taraf bunu
    /// "damga yok" gibi ele alır, yani bozuk bir kayıt isteği kilitlemez.
    ///
    /// SARAN TIRNAKLAR NEDEN SOYULUYOR: yazan taraf IdentityService'in
    /// <c>ICacheService</c>'i üzerinden geçiyor ve o değeri JSON'a çeviriyor —
    /// bir string Redis'e <c>"1789822227|roles"</c> biçiminde, tırnaklı giriyor.
    /// Okuyan taraf (middleware) ise ham okuyor. Bu asimetri sessizce ayrıştırma
    /// hatasına ve kontrolün kendi kendini kapatmasına yol açıyordu; ilk denemede
    /// tam olarak bu oldu. Biçimin iki ucu da burada dursun diye tırnak temizliği
    /// sözleşmenin kendi içinde yapılıyor.
    /// </summary>
    public static bool TryParse(string? value, out long invalidBeforeUnix, out string reason)
    {
        invalidBeforeUnix = 0;
        reason = ReasonRoles;

        if (string.IsNullOrEmpty(value)) return false;

        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            value = value[1..^1];

        var separator = value.IndexOf('|');
        if (separator <= 0) return false;

        if (!long.TryParse(value[..separator], out invalidBeforeUnix)) return false;

        reason = value[(separator + 1)..];
        return true;
    }
}
