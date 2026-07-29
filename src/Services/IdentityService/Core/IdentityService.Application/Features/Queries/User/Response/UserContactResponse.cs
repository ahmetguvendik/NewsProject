namespace IdentityService.Application.Features.Queries.User.Response;

/// <summary>
/// E-posta içerir — GetUserDirectoryQuery'nin herkese açık UserDirectoryEntryResponse'ından
/// farklı olarak bu DTO yalnızca Internal:ApiKey ile korunan uçtan dönmeli.
/// </summary>
public class UserContactResponse
{
    public string KeycloakId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
