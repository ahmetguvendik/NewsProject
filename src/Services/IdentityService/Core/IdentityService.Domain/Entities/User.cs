using IdentityService.Domain.Common;

namespace IdentityService.Domain.Entities;

public class User : BaseEntity
{
    public string KeycloakId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Bülten aboneliği. Opt-in: kayıt sırasında false, kullanıcı kendisi açar.
    /// Haber yayınlanırken "abonelere bildir" işaretliyse bu kullanıcılara mail gider.
    /// </summary>
    public bool IsSubscribed { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
