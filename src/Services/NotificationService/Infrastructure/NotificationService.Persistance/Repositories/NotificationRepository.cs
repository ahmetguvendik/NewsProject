using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using NotificationService.Persistance.Contexts;

namespace NotificationService.Persistance.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationServiceDbContext _context;

    public NotificationRepository(NotificationServiceDbContext context)
    {
        _context = context;
    }

    public Task CreateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        _context.Notifications.Add(notification);
        return Task.CompletedTask;
    }
}
