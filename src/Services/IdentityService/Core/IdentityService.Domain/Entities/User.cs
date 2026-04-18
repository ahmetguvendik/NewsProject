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

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
