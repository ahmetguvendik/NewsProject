using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;

namespace NewsOutboxWorker;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;
    private readonly int _maxRetryCount;
    private readonly int _retentionDays;
    private readonly int _batchSize;
    private readonly TimeSpan _pollInterval;

    /// <summary>Temizlik her turda değil, bu aralıkta bir çalışır.</summary>
    private readonly TimeSpan _cleanupInterval;

    private DateTime _lastCleanupAt = DateTime.MinValue;

    public Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
        _maxRetryCount = _configuration.GetValue("Outbox:MaxRetryCount", 5);
        _retentionDays = _configuration.GetValue("Outbox:RetentionDays", 7);
        _batchSize = _configuration.GetValue("Outbox:BatchSize", 50);
        _pollInterval = TimeSpan.FromSeconds(_configuration.GetValue("Outbox:PollIntervalSeconds", 5));
        _cleanupInterval = TimeSpan.FromHours(_configuration.GetValue("Outbox:CleanupIntervalHours", 1));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NewsOutboxWorker started.");

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
                await CleanupProcessedMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in NewsOutboxWorker loop.");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("NewsOutboxWorker stopped.");
    }

    private async Task ProcessOutboxMessagesAsync(IProducer<string, string> producer, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OutboxWorkerDbContext>();

        var messages = await db.OutboxMessages
            .Where(m => !m.IsProcessed && !m.IsDeadLettered)
            .OrderBy(m => m.CreatedAt)
            .Take(_batchSize)
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

    /// <summary>
    /// Kafka'ya iletilmiş satırların tabloda kalması için bir sebep yok; birikirlerse
    /// her turdaki "işlenmemişleri getir" sorgusu giderek daha fazla ölü satırın
    /// üzerinden geçer. Dead-letter satırları KORUNUR — incelenmeleri gerekiyor.
    /// </summary>
    private async Task CleanupProcessedMessagesAsync(CancellationToken cancellationToken)
    {
        if (DateTime.UtcNow - _lastCleanupAt < _cleanupInterval)
            return;

        _lastCleanupAt = DateTime.UtcNow;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OutboxWorkerDbContext>();

        var cutoff = DateTime.UtcNow.AddDays(-_retentionDays);

        var deleted = await db.OutboxMessages
            .Where(m => m.IsProcessed && m.ProcessedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
            _logger.LogInformation("Cleaned up {Count} processed outbox messages older than {Cutoff:u}.", deleted, cutoff);
    }
}
