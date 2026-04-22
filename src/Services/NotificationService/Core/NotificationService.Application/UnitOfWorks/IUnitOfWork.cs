namespace NotificationService.Application.UnitOfWorks;

public interface IUnitOfWork : IDisposable
{
    /// <summary>Bekleyen tüm değişiklikleri DB'ye yazar.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Explicit transaction başlatır.</summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Açık transaction'ı commit eder.</summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Açık transaction'ı geri alır.</summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
