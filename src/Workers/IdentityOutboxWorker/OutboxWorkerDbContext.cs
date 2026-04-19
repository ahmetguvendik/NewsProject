using IdentityService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IdentityOutboxWorker;

public class OutboxWorkerDbContext : DbContext
{
    public OutboxWorkerDbContext(DbContextOptions<OutboxWorkerDbContext> options) : base(options) { }

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
}
