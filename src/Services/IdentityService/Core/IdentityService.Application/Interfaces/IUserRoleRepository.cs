using IdentityService.Domain.Entities;

namespace IdentityService.Application.Interfaces;

public interface IUserRoleRepository
{
    Task<List<UserRole>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserRole?> GetAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task CreateAsync(UserRole userRole, CancellationToken cancellationToken = default);
    Task DeleteAsync(UserRole userRole, CancellationToken cancellationToken = default);
    // Pasif adminler sayılmaz: sisteme giremedikleri için kimseyi yönetemezler.
    Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default);
}
