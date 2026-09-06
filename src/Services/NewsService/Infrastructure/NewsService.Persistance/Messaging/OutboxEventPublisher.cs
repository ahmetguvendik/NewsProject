using NewsService.Application.Interfaces;
using NewsService.Domain.Entities;
using NewsService.Persistance.Contexts;
using System.Diagnostics;
using System.Text.Json;

namespace NewsService.Persistance.Messaging;

public class OutboxEventPublisher : IEventPublisher
{
    private readonly NewsServiceDbContext _context;

    public OutboxEventPublisher(NewsServiceDbContext context)
    {
        _context = context;
    }

    public Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        _context.OutboxMessages.Add(new OutboxMessage
        {
            Topic = topic,
            Payload = JsonSerializer.Serialize(message),

            // İsteğin trace bağlamı satırla birlikte kaydediliyor. Worker saniyeler
            // sonra ayrı bir process'te uyandığında Activity çoktan ölmüş oluyor;
            // taşımanın tek yolu satırın kendisi. Activity.Current?.Id, W3C
            // traceparent biçiminde ("00-{trace}-{span}-{flags}").
            TraceParent = Activity.Current?.Id
        });

        // SaveChanges YOK — handler'daki UnitOfWork.SaveChangesAsync() entity ile birlikte kaydeder
        return Task.CompletedTask;
    }
}
