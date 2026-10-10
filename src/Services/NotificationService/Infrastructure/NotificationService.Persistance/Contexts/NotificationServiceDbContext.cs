using Microsoft.EntityFrameworkCore;
using NotificationService.Domain.Entities;

namespace NotificationService.Persistance.Contexts;

public class NotificationServiceDbContext : DbContext
{
    public NotificationServiceDbContext(DbContextOptions<NotificationServiceDbContext> options) : base(options) { }

    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Aynı Kafka mesajı iki kez yazılamasın: tekrar kontrolünün veritabanı güvencesi.
        modelBuilder.Entity<InboxMessage>()
            .HasIndex(m => m.MessageId)
            .IsUnique();

        modelBuilder.Entity<InboxMessage>()
            .HasIndex(m => m.ReceivedAt)
            .HasFilter("\"IsProcessed\" = false AND \"IsDeadLettered\" = false");

        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.ReferenceId, n.Type });
    }
}
