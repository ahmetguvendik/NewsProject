using System.Linq.Expressions;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Common;
using IdentityService.Persistance.Contexts;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Persistance.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : BaseEntity
{
    private readonly IdentityServiceDbContext _context;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(IdentityServiceDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbSet.Where(x => !x.IsDeleted).ToListAsync(cancellationToken);

    public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.Where(x => !x.IsDeleted).Where(predicate).ToListAsync(cancellationToken);

    public async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id) && !x.IsDeleted, cancellationToken);

    public async Task CreateAsync(T entity, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(entity, cancellationToken);

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public IQueryable<T> GetQueryable()
        => _dbSet.Where(x => !x.IsDeleted).AsQueryable();
}
