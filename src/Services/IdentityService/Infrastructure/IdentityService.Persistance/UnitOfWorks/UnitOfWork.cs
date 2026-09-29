using System.Data;
using IdentityService.Application.UnitOfWorks;
using IdentityService.Persistance.Contexts;
using Microsoft.EntityFrameworkCore;
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

    public async Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
    {
        // İç içe transaction desteklenmiyor. Sessizce ikincisini açıp ilkinin
        // referansını kaybetmek, ilkini asla commit/rollback edilmeyen açık bir
        // transaction olarak bırakırdı.
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
            // Commit başarısız olsa da (Serializable çakışması tam burada
            // çıkabiliyor) transaction bitmiş sayılır; referans tutulursa bir
            // sonraki Begin "zaten açık" diye reddedilirdi.
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

    /// <summary>
    /// DbContext'e DOKUNMUYOR: onun sahibi DI container, scope kapanınca kendisi
    /// dispose ediyor. Önceki hâli context'i de dispose ediyordu — aynı scope'ta
    /// UnitOfWork'ten sonra context'i kullanan her şey ObjectDisposedException
    /// alırdı. Yalnızca commit/rollback edilmeden kalmış transaction kapatılıyor
    /// (Postgres onu geri alır).
    /// </summary>
    public void Dispose()
    {
        _transaction?.Dispose();
        _transaction = null;
    }
}
