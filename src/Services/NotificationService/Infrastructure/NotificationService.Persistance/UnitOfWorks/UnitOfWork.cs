using Microsoft.EntityFrameworkCore.Storage;
using NotificationService.Application.UnitOfWorks;
using NotificationService.Persistance.Contexts;

namespace NotificationService.Persistance.UnitOfWorks;

public class UnitOfWork : IUnitOfWork
{
    private readonly NotificationServiceDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(NotificationServiceDbContext context)
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
