namespace IdentityService.Application.Caching;

/// <summary>
/// Önbellek anahtarları tek yerde tutulur: okuyan sorgu ile geçersiz kılan
/// komut aynı sabiti kullanmazsa, yazma sonrası bayat veri servis edilir ve
/// bu tür bir hata sessizce ilerler.
///
/// Anahtarlar servise ait; NewsService'in <c>news:</c> önekli sabitleriyle
/// paylaşılan bir yerde toplanmıyor. Bir servisin diğerinin anahtar listesini
/// görmesi, yanlış taraftan geçersizleştirme yapmayı kolaylaştırır.
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Keycloak ID → görünen ad. Kullanıcı başına bir alan tutulur; istek başına
    /// değil, çünkü dizin sorgusu keyfi bir ID listesi alıyor ve istek bazlı
    /// anahtarlamada anahtar uzayı kombinatoryal büyüyüp isabet oranı çökerdi.
    /// </summary>
    public const string Directory = "identity:directory";

    /// <summary>Kullanıcının kendi profili — anahtara Keycloak ID eklenir.</summary>
    public static string Profile(string keycloakId) => $"identity:profile:{keycloakId}";
}
