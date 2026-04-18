using NewsService.Application.UnitOfWorks;
using NewsService.Persistance.Contexts;

namespace NewsService.Persistance.UnitOfWorks;

public class UnitOfWork : IUnitOfWork
{
    private readonly NewsServiceDbContext _context;

    public UnitOfWork(NewsServiceDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _context.SaveChangesAsync(cancellationToken);
}
