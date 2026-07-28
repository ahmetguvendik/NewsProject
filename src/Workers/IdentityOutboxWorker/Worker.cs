using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;

namespace IdentityOutboxWorker;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly int _maxRetryCount;

    public Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
        _maxRetryCount = _configuration.GetValue("Outbox:MaxRetryCount", 5);
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
            .Where(m => !m.IsProcessed && !m.IsDeadLettered)
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
                message.RetryCount++;
                message.Error = ex.Message;

                if (message.RetryCount >= _maxRetryCount)
                {
                    message.IsDeadLettered = true;
                    _logger.LogError(ex, "Outbox message {Id} dead-lettered after {RetryCount} attempts on topic '{Topic}'.",
                        message.Id, message.RetryCount, message.Topic);
                }
                else
                {
                    _logger.LogWarning(ex, "Failed to publish outbox message {Id} to topic '{Topic}' (attempt {RetryCount}/{Max}).",
                        message.Id, message.Topic, message.RetryCount, _maxRetryCount);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
