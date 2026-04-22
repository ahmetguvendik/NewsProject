using NotificationService.Domain.Entities;

namespace NotificationService.Application.Interfaces;

public interface INotificationRepository
{
    Task CreateAsync(Notification notification, CancellationToken cancellationToken = default);
}
