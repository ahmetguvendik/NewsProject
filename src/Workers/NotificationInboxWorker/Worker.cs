using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using Shared.Messaging;
using Shared.Messaging.Events;
using System.Text.Json;

namespace NotificationInboxWorker;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;
    private readonly int _maxRetryCount;

    public Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger, IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _maxRetryCount = configuration.GetValue("Inbox:MaxRetryCount", 5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationInboxWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessInboxMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in NotificationInboxWorker.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task ProcessInboxMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InboxWorkerDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var identityContactClient = scope.ServiceProvider.GetRequiredService<IIdentityContactClient>();

        var messages = await db.InboxMessages
            .Where(m => !m.IsProcessed && !m.IsDeadLettered)
            .OrderBy(m => m.ReceivedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        _logger.LogInformation("Processing {Count} inbox messages.", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                await HandleMessageAsync(message, emailService, identityContactClient, db, cancellationToken);
                message.IsProcessed = true;
                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;

                if (message.RetryCount >= _maxRetryCount)
                {
                    message.IsDeadLettered = true;
                    _logger.LogError(ex, "Inbox message {Id} dead-lettered after {RetryCount} attempts on topic '{Topic}'.",
                        message.Id, message.RetryCount, message.Topic);
                }
                else
                {
                    _logger.LogWarning(ex, "Failed to process inbox message {Id} for topic '{Topic}' (attempt {RetryCount}/{Max}).",
                        message.Id, message.Topic, message.RetryCount, _maxRetryCount);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleMessageAsync(
        InboxMessage message,
        IEmailService emailService,
        IIdentityContactClient identityContactClient,
        InboxWorkerDbContext db,
        CancellationToken cancellationToken)
    {
        if (message.Topic == Topics.User.Registered)
        {
            var evt = JsonSerializer.Deserialize<UserRegisteredEvent>(message.Payload)!;
            await SendAndSaveAsync(emailService, db,
                type: "welcome",
                recipient: evt.Email,
                subject: "Telgraf'a Hoşgeldiniz! 🎉",
                body: $"""
                    <html><body style="font-family:Arial,sans-serif;padding:20px">
                        <h2>Merhaba {evt.FirstName} {evt.LastName},</h2>
                        <p>Telgraf'a hoşgeldiniz! Hesabınız başarıyla oluşturuldu.</p>
                        <p>Artık haberleri okuyabilir ve daha fazlasını keşfedebilirsiniz.</p>
                        <br/><p>İyi okumalar,</p><p><strong>Telgraf Ekibi</strong></p>
                    </body></html>
                    """,
                cancellationToken);
        }
        else if (message.Topic == Topics.Article.Published)
        {
            var evt = JsonSerializer.Deserialize<ArticlePublishedEvent>(message.Payload)!;

            // Event'te yalnızca AuthorKeycloakId var — gerçek e-posta IdentityService'ten
            // canlı çözülüyor. Kullanıcı bulunamazsa (silinmiş vb.) exception fırlatılır,
            // mesaj retry/dead-letter mekanizmasına düşer.
            var contact = await identityContactClient.GetContactAsync(evt.AuthorKeycloakId, cancellationToken)
                ?? throw new InvalidOperationException($"Yazarın iletişim bilgisi bulunamadı: {evt.AuthorKeycloakId}");

            await SendAndSaveAsync(emailService, db,
                type: "article_published",
                recipient: contact.Email,
                subject: $"Haberiniz Yayınlandı: {evt.Title}",
                body: $"""
                    <html><body style="font-family:Arial,sans-serif;padding:20px">
                        <h2>Merhaba {contact.FirstName},</h2>
                        <p>"<strong>{evt.Title}</strong>" başlıklı haberiniz yayına alındı! 📰</p>
                        <p><strong>Yayın Tarihi:</strong> {ToTurkeyLocalTime(evt.PublishedAt):dd MMMM yyyy HH:mm}</p>
                        <br/><p><strong>Telgraf Ekibi</strong></p>
                    </body></html>
                    """,
                cancellationToken);
        }
        else
        {
            _logger.LogWarning("Unknown topic '{Topic}', skipping.", message.Topic);
        }
    }

    /// <summary>
    /// DB ve event'lerde her zaman UTC tutulur; dönüştürme yalnızca insana gösterilecek
    /// noktada (mail metni) yapılır. Türkiye 2016'dan beri yaz saati uygulamadığı için
    /// sabit +3 offset yeterli — TimeZoneInfo/tzdata bağımlılığı gerektirmiyor.
    /// </summary>
    private static DateTime ToTurkeyLocalTime(DateTime utc) => utc.AddHours(3);

    private async Task SendAndSaveAsync(IEmailService emailService, InboxWorkerDbContext db,
        string type, string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        var notification = new Notification
        {
            Type = type,
            RecipientEmail = recipient,
            Subject = subject,
            Body = body
        };

        await emailService.SendAsync(recipient, subject, body, cancellationToken);
        notification.IsSent = true;
        notification.SentAt = DateTime.UtcNow;

        db.Notifications.Add(notification);

        _logger.LogInformation("Email sent to {Recipient} for type '{Type}'.", recipient, type);
    }
}
