using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NewsService.Application.UnitOfWorks;
using NewsService.Persistance.Contexts;

namespace NewsService.Persistance.UnitOfWorks;

public class UnitOfWork : IUnitOfWork
{
    private readonly NewsServiceDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(NewsServiceDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);

    public async Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("Zaten açık bir transaction var.");

        _transaction = await _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await EndAsync();
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
            return;

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await EndAsync();
        }
    }

    private async Task EndAsync()
    {
        await _transaction!.DisposeAsync();
        _transaction = null;
    }

    // DbContext'i DI yönetiyor; burada yalnızca açık kalan transaction kapatılır.
    public void Dispose()
    {
        _transaction?.Dispose();
        _transaction = null;
    }
}
