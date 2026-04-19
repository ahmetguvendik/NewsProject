using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Persistance.Contexts;
using Shared.Messaging;

namespace NotificationService.Persistance.Consumers;

public class ArticlePublishedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ArticlePublishedConsumer> _logger;

    public ArticlePublishedConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<ArticlePublishedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            GroupId = "notification-service-article",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(Topics.Article.Published);

        _logger.LogInformation("ArticlePublishedConsumer started, listening to '{Topic}'.", Topics.Article.Published);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result is null) continue;

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NotificationServiceDbContext>();

                // Duplicate kontrolü
                var alreadyExists = db.InboxMessages.Any(m => m.MessageId == result.Message.Key);
                if (!alreadyExists)
                {
                    db.InboxMessages.Add(new InboxMessage
                    {
                        MessageId = result.Message.Key,
                        Topic = result.Topic,
                        Payload = result.Message.Value
                    });

                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("InboxMessage saved for topic '{Topic}', key '{Key}'.", result.Topic, result.Message.Key);
                }
                else
                {
                    _logger.LogWarning("Duplicate message skipped. Key: {Key}", result.Message.Key);
                }

                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ArticlePublishedConsumer.");
                await Task.Delay(3000, stoppingToken);
            }
        }

        consumer.Close();
    }
}
