using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;

namespace NotificationInboxWorker;

public class InboxWorkerDbContext : DbContext
{
    public InboxWorkerDbContext(DbContextOptions<InboxWorkerDbContext> options) : base(options) { }

    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<Notification> Notifications => Set<Notification>();
}
