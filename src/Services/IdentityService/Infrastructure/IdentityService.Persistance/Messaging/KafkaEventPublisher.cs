using Confluent.Kafka;
using IdentityService.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IdentityService.Persistance.Messaging;

public class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    public KafkaEventPublisher(IConfiguration configuration, ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;

        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:29092";

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(message);

            var result = await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = json
            }, cancellationToken);

            _logger.LogInformation("Event published to topic '{Topic}', partition {Partition}, offset {Offset}",
                topic, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish event to topic '{Topic}'. Error: {Error}", topic, ex.Error.Reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while publishing event to topic '{Topic}'", topic);
        }
    }

    public void Dispose() => _producer.Dispose();
}
