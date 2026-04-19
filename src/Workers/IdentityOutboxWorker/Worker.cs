using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;

namespace IdentityOutboxWorker;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    public Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IdentityOutboxWorker started.");

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092"
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxMessagesAsync(producer, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in OutboxWorker loop.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("IdentityOutboxWorker stopped.");
    }

    private async Task ProcessOutboxMessagesAsync(IProducer<string, string> producer, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OutboxWorkerDbContext>();

        var messages = await db.OutboxMessages
            .Where(m => !m.IsProcessed)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        _logger.LogInformation("Processing {Count} outbox messages...", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                await producer.ProduceAsync(
                    message.Topic,
                    new Message<string, string>
                    {
                        Key = message.Id.ToString(),
                        Value = message.Payload
                    },
                    cancellationToken);

                message.IsProcessed = true;
                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;

                _logger.LogInformation("Published outbox message {Id} to topic '{Topic}'.", message.Id, message.Topic);
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                _logger.LogError(ex, "Failed to publish outbox message {Id} to topic '{Topic}'.", message.Id, message.Topic);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
