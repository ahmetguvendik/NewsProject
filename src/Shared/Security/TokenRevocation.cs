using System.Text.Json;

namespace Shared.Security;

/// <summary>
/// Bir kullanıcının elindeki token'ların geçersiz kılındığını söyleyen kayıt.
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
/// İkincisi birincinin kontrolüne takılmıyordu: kullanıcı "pasif" değil, sadece
/// rütbesi indirilmiş.
///
/// ÇÖZÜM: yetki etkileyen bir şey değiştiğinde IdentityService buraya bir kayıt
/// bırakıyor. Servisler her istekte token'ın <c>iat</c> değerine bakıp
/// <see cref="Covers"/> ile karşılaştırıyor.
///
/// NEDEN ZAMAN, "VAR/YOK" BAYRAĞI DEĞİL: kullanıcı tekrar giriş yaptığında yeni
/// token'ın iat'i kayıttan sonra doğar ve kendiliğinden geçerli sayılır. Bayrak
/// olsaydı, rolü değişen kullanıcı TTL dolana kadar giriş yapsa bile içeri
/// alınamazdı.
///
/// NEDEN Keycloak'A SORMUYORUZ: introspection "oturum yaşıyor mu" diye cevap
/// veriyor. Rol kaldırmak oturumu öldürmediği için tam da yakalamak istediğimiz
/// vakada <c>active: true</c> döner. Üstelik her istekte ağ turu demek ve
/// Keycloak'ı sıcak yola zorunlu bağımlılık haline getirirdi.
///
/// NEDEN Shared'DA: servis sınırını geçen bir sözleşme. Yazan IdentityService,
/// okuyan NewsService ve IdentityService'in kendisi.
/// </summary>
public sealed record TokenRevocation(DateTimeOffset RevokedAt, string Reason)
{
    /// <summary>Hesap pasife alındı. Tekrar giriş çözmez — Keycloak da engelliyor.</summary>
    public const string ReasonAccountDisabled = "account-disabled";

    /// <summary>Rol atandı veya kaldırıldı. Tekrar giriş yapmak çözer.</summary>
    public const string ReasonRoleChange = "role-change";

    /// <summary>
    /// Anahtar. Biçim <c>identity:profile:{id}</c> ve <c>identity:directory</c>
    /// ile aynı kalıpta: ad alanı, tek kelimelik ad, kimlik.
    /// </summary>
    public static string Key(string keycloakId) => $"identity:revoked:{keycloakId}";

    /// <summary>
    /// Kaydın Redis'te kalma süresi.
    ///
    /// Kalıcı olmasına gerek YOK: tek işi, yazıldığı anda hayatta olan
    /// token'ların ömrünü doldurmasını beklemek. O süre geçtikten sonra zaten
    /// hiçbir eski token geçerli değil.
    ///
    /// Realm'deki accessTokenLifespan şu an 3600 sn. O değer DÜŞÜRÜLÜRSE burası
    /// olduğu gibi kalabilir (fazladan beklemek zararsız); YÜKSELTİLİRSE burası
    /// da yükseltilmeli, yoksa arada kontrolsüz bir pencere açılır.
    /// </summary>
    public static readonly TimeSpan Retention = TimeSpan.FromHours(2);

    /// <summary>
    /// Yazan ve okuyan tarafın AYNI ayarları kullanması şart.
    ///
    /// Yazan taraf IdentityService'in <c>ICacheService</c>'i, okuyan taraf ise
    /// middleware — ikisi farklı katmanda. İlk sürümde değer düz metin olarak
    /// yazılıp ham okunuyordu; önbellek onu JSON'a çevirince Redis'e tırnaklı
    /// girdi, okuyan taraf ayrıştıramadı ve kontrol SESSİZCE kendini kapattı.
    /// Ayarın burada, sözleşmenin yanında durması o tuzağı kapatıyor.
    ///
    /// <see cref="JsonSerializerDefaults.Web"/> seçildi çünkü önbelleğin
    /// kullandığı ayar da bu.
    /// </summary>
    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Şimdi geçersiz kılınmış bir kayıt üretir.
    ///
    /// Saniyeye yuvarlanıyor: karşılaştırılacak olan <c>iat</c> zaten saniye
    /// hassasiyetinde, ayrıca redis-cli'de okunan değer kısa ve temiz kalıyor.
    /// </summary>
    public static TokenRevocation Now(string reason) =>
        new(DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds()), reason);

    /// <summary>
    /// Redis'ten okunan ham değeri çözer. Bozuk veya boşsa <c>null</c> döner —
    /// çağıran taraf bunu "kayıt yok" gibi ele alır, yani bozuk bir satır
    /// isteği kilitlemez.
    /// </summary>
    public static TokenRevocation? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        try
        {
            var revocation = JsonSerializer.Deserialize<TokenRevocation>(raw, SerializerOptions);
            return string.IsNullOrEmpty(revocation?.Reason) ? null : revocation;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Verilen token bu kaydın kapsamında mı? <c>tokenIssuedAtUnix</c>, token'ın
    /// <c>iat</c> değeri (Unix saniye). Kayıttan SONRA üretilmiş token geçerli:
    /// kullanıcı tekrar giriş yapmış demektir.
    /// </summary>
    public bool Covers(long tokenIssuedAtUnix) => tokenIssuedAtUnix <= RevokedAt.ToUnixTimeSeconds();
}
