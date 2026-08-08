namespace IdentityService.Application.Features.Queries.User.Response;

/// <summary>Kullanıcının kendi profili — /api/me üzerinden döner.</summary>
public class MyProfileResponse
{
    public Guid Id { get; set; }
    public string KeycloakId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsSubscribed { get; set; }
}
