namespace IdentityService.Application.Features.Queries.User.Response;

/// <summary>
/// Bilerek minimal tutulur — bu uç nokta [AllowAnonymous]; e-posta, rol,
/// aktiflik gibi hassas alanlar buradan asla dönmemeli.
/// </summary>
public class UserDirectoryEntryResponse
{
    public string KeycloakId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}
