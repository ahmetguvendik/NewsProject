using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Domain.Entities;
using NotificationService.Persistance.Contexts;
using Shared.Messaging;

namespace NotificationService.Persistance.Consumers;

public class UserRegisteredConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserRegisteredConsumer> _logger;

    public UserRegisteredConsumer(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<UserRegisteredConsumer> logger)
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
            GroupId = "notification-service-user",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(Topics.User.Registered);

        _logger.LogInformation("UserRegisteredConsumer started, listening to '{Topic}'.", Topics.User.Registered);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                if (result is null) continue;

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NotificationServiceDbContext>();

                // Duplicate kontrolü — aynı mesaj daha önce geldi mi?
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

                // DB'ye kaydedildikten sonra offset commit et
                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UserRegisteredConsumer.");
                await Task.Delay(3000, stoppingToken);
            }
        }

        consumer.Close();
    }
}
