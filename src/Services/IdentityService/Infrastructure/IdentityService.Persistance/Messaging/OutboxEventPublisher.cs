using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using IdentityService.Persistance.Contexts;
using System.Text.Json;

namespace IdentityService.Persistance.Messaging;

public class OutboxEventPublisher : IEventPublisher
{
    private readonly IdentityServiceDbContext _context;

    public OutboxEventPublisher(IdentityServiceDbContext context)
    {
        _context = context;
    }

    public Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        var outboxMessage = new OutboxMessage
        {
            Topic = topic,
            Payload = JsonSerializer.Serialize(message)
        };

        _context.OutboxMessages.Add(outboxMessage);

        // SaveChanges YOK — handler'daki UnitOfWork.SaveChangesAsync() entity ile birlikte kaydeder
        return Task.CompletedTask;
    }
}
