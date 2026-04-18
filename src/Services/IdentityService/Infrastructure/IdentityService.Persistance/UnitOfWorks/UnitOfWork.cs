using IdentityService.Application.UnitOfWorks;
using IdentityService.Persistance.Contexts;
using Microsoft.EntityFrameworkCore.Storage;

namespace IdentityService.Persistance.UnitOfWorks;

public class UnitOfWork : IUnitOfWork
{
    private readonly IdentityServiceDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(IdentityServiceDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        => _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            await _transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            await _transaction.RollbackAsync(cancellationToken);
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
