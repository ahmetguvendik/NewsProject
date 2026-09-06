using Confluent.Kafka;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NotificationService.Application.UnitOfWorks;
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
            GroupId = _configuration["Kafka:ConsumerGroups:Article"] ?? "notification-service-article",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(Topics.Article.Published);

        _logger.LogInformation("ArticlePublishedConsumer started, listening to '{Topic}'.", Topics.Article.Published);

        // Consume timeout'u ayarlanabilir değil: kapanma sinyalinin en geç ne kadar sürede
        // fark edileceğini belirliyor, tuning değeri değil.
        var pollTimeout = TimeSpan.FromSeconds(1);
        var errorBackoff = TimeSpan.FromSeconds(_configuration.GetValue("Kafka:ErrorBackoffSeconds", 3));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = consumer.Consume(pollTimeout);
                if (result is null) continue;

                // Outbox worker'ın mesaja koyduğu traceparent okunuyor ve Activity
                // geri kuruluyor; bu bloktaki loglar isteği başlatan trace ile aynı
                // kimliği taşısın diye. Değer ayrıca InboxMessage'a yazılıyor, çünkü
                // asıl işi yapan worker ayrı bir process ve saniyeler sonra çalışıyor.
                var traceParent = ReadTraceParent(result.Message.Headers);

                var activity = new Activity("inbox.receive");
                if (!string.IsNullOrEmpty(traceParent))
                    activity.SetParentId(traceParent);
                using var startedActivity = activity.Start();

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<NotificationServiceDbContext>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // Duplicate kontrolü
                var alreadyExists = db.InboxMessages.Any(m => m.MessageId == result.Message.Key);
                if (!alreadyExists)
                {
                    db.InboxMessages.Add(new InboxMessage
                    {
                        MessageId = result.Message.Key,
                        Topic = result.Topic,
                        Payload = result.Message.Value,
                        TraceParent = traceParent
                    });

                    await unitOfWork.SaveChangesAsync(stoppingToken);
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
                await Task.Delay(errorBackoff, stoppingToken);
            }
        }

        consumer.Close();
    }

    /// <summary>
    /// Kafka başlığındaki traceparent. Yoksa null döner — outbox'ta TraceParent
    /// boş olan eski satırlar için normal.
    /// </summary>
    private static string? ReadTraceParent(Headers? headers)
    {
        if (headers is null || !headers.TryGetLastBytes("traceparent", out var bytes))
            return null;

        return System.Text.Encoding.UTF8.GetString(bytes);
    }
}
