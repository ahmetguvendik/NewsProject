using NotificationService.Application.UnitOfWorks;
using NotificationService.Persistance.Contexts;

namespace NotificationService.Persistance.UnitOfWorks;

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
