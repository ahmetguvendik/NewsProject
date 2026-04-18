using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using IdentityService.Persistance.Contexts;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Persistance.Repositories;

public class UserRoleRepository : IUserRoleRepository
{
    private readonly IdentityServiceDbContext _context;

    public UserRoleRepository(IdentityServiceDbContext context)
    {
        _context = context;
    }

    public async Task<List<UserRole>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.UserRoles.Where(ur => ur.UserId == userId).ToListAsync(cancellationToken);

    public async Task<UserRole?> GetAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        => await _context.UserRoles.FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

    public async Task CreateAsync(UserRole userRole, CancellationToken cancellationToken = default)
        => await _context.UserRoles.AddAsync(userRole, cancellationToken);

    public Task DeleteAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        _context.UserRoles.Remove(userRole);
        return Task.CompletedTask;
    }

    public IQueryable<UserRole> GetQueryable()
        => _context.UserRoles.AsQueryable();
}
