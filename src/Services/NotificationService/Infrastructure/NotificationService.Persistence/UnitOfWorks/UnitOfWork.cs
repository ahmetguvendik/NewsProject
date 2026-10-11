using NotificationService.Application.UnitOfWorks;
using NotificationService.Persistence.Contexts;

namespace NotificationService.Persistence.UnitOfWorks;

public class UnitOfWork : IUnitOfWork
{
    private readonly NotificationServiceDbContext _context;

    public UnitOfWork(NotificationServiceDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
