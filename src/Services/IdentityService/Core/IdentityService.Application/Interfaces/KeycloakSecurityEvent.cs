namespace IdentityService.Application.Interfaces;

/// <summary>
/// Keycloak'ın ürettiği bir güvenlik olayı.
///
/// Parola sıfırlama tamamen Keycloak'ın içinde geçiyor — istek, jeton, mail ve
/// yeni parola formu hep orada. Bizim servislerimiz bu akışı hiç görmüyor,
/// dolayısıyla loglarımızda da izi yoktu. Bu tür, olayları kendi log hattımıza
/// taşımak için Keycloak'ın olay API'sinden okunan alanları taşıyor.
///
/// KEYCLOAK'IN <c>details</c> SÖZLÜĞÜ BİLEREK ALINMIYOR: içinde <c>email</c> ve
/// <c>username</c> var, yani açık e-posta adresi. Loglardan kişisel veriyi
/// çıkarıp yerine Keycloak kimliği koyma kararı daha önce verilmişti; buradan
/// geri sızmasın diye yalnızca <see cref="UserId"/> taşınıyor.
/// </summary>
public sealed class KeycloakSecurityEvent
{
    /// <summary>Olayın oluştuğu an (Unix epoch, milisaniye).</summary>
    public long Time { get; init; }

    /// <summary>Olay türü: SEND_RESET_PASSWORD, RESET_PASSWORD, UPDATE_PASSWORD…</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Olayın ilgili olduğu kullanıcının Keycloak kimliği. Eşleşen kullanıcı yoksa boş.</summary>
    public string? UserId { get; init; }

    /// <summary>İsteğin geldiği adres.</summary>
    public string? IpAddress { get; init; }

    /// <summary>İsteği başlatan istemci (news-portal-client).</summary>
    public string? ClientId { get; init; }
}
