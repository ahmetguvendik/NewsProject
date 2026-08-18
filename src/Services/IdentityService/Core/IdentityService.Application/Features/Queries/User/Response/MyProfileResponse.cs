namespace IdentityService.Application.Features.Queries.User.Response;

/// <summary>
/// Kullanıcının kendi profili — /api/me üzerinden döner.
///
/// Bu yanıt önbelleklenir (bkz. <c>GetMyProfileQuery</c>). Buraya <b>yeni bir
/// alan eklerken</b>, o alanı değiştiren komutun da profili düşürdüğünden emin
/// olun. Örneğin roller şu an burada dönmüyor; bu yüzden rol atama/kaldırma
/// komutları önbelleğe dokunmuyor. Roller eklenirse o iki handler'a da
/// geçersizleştirme girmeli, aksi halde kullanıcı rol değişikliğini TTL
/// dolana kadar görmez.
/// </summary>
public class MyProfileResponse
{
    public Guid Id { get; set; }
    public string KeycloakId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsSubscribed { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
