using Microsoft.EntityFrameworkCore;
using NewsService.Domain.Entities;

namespace NewsOutboxWorker;

public class OutboxWorkerDbContext : DbContext
{
    public OutboxWorkerDbContext(DbContextOptions<OutboxWorkerDbContext> options) : base(options) { }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
}
