using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using IdentityService.Persistance.Contexts;
using System.Diagnostics;
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
            Payload = JsonSerializer.Serialize(message),

            // İsteğin trace bağlamı satırla birlikte kaydediliyor. Worker saniyeler
            // sonra ayrı bir process'te uyandığında Activity çoktan ölmüş oluyor;
            // taşımanın tek yolu satırın kendisi. Activity.Current?.Id, W3C
            // traceparent biçiminde ("00-{trace}-{span}-{flags}").
            TraceParent = Activity.Current?.Id
        };

        _context.OutboxMessages.Add(outboxMessage);

        // SaveChanges YOK — handler'daki UnitOfWork.SaveChangesAsync() entity ile birlikte kaydeder
        return Task.CompletedTask;
    }
}
