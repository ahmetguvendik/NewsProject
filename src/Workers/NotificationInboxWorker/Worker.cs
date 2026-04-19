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

    public Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
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

        var messages = await db.InboxMessages
            .Where(m => !m.IsProcessed)
            .OrderBy(m => m.ReceivedAt)
            .Take(50)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        _logger.LogInformation("Processing {Count} inbox messages.", messages.Count);

        foreach (var message in messages)
        {
            try
            {
                await HandleMessageAsync(message, emailService, db, cancellationToken);
                message.IsProcessed = true;
                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.Error = ex.Message;
                _logger.LogError(ex, "Failed to process inbox message {Id} for topic '{Topic}'.", message.Id, message.Topic);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task HandleMessageAsync(InboxMessage message, IEmailService emailService, InboxWorkerDbContext db, CancellationToken cancellationToken)
    {
        if (message.Topic == Topics.User.Registered)
        {
            var evt = JsonSerializer.Deserialize<UserRegisteredEvent>(message.Payload)!;
            await SendAndSaveAsync(emailService, db,
                type: "welcome",
                recipient: evt.Email,
                subject: "News Portal'a Hoşgeldiniz! 🎉",
                body: $"""
                    <html><body style="font-family:Arial,sans-serif;padding:20px">
                        <h2>Merhaba {evt.FirstName} {evt.LastName},</h2>
                        <p>News Portal'a hoşgeldiniz! Hesabınız başarıyla oluşturuldu.</p>
                        <p>Artık haberleri okuyabilir ve daha fazlasını keşfedebilirsiniz.</p>
                        <br/><p>İyi okumalar,</p><p><strong>News Portal Ekibi</strong></p>
                    </body></html>
                    """,
                cancellationToken);
        }
        else if (message.Topic == Topics.Article.Published)
        {
            var evt = JsonSerializer.Deserialize<ArticlePublishedEvent>(message.Payload)!;
            await SendAndSaveAsync(emailService, db,
                type: "article_published",
                recipient: evt.AuthorKeycloakId, // ileride gerçek e-posta eklenecek
                subject: $"Yeni Haber Yayınlandı: {evt.Title}",
                body: $"""
                    <html><body style="font-family:Arial,sans-serif;padding:20px">
                        <h2>Yeni bir haber yayınlandı! 📰</h2>
                        <p><strong>Başlık:</strong> {evt.Title}</p>
                        <p><strong>Yayın Tarihi:</strong> {evt.PublishedAt:dd MMMM yyyy HH:mm}</p>
                        <br/><p><strong>News Portal Ekibi</strong></p>
                    </body></html>
                    """,
                cancellationToken);
        }
        else
        {
            _logger.LogWarning("Unknown topic '{Topic}', skipping.", message.Topic);
        }
    }

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
