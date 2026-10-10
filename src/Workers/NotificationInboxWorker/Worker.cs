using System.Diagnostics;
using Logging.Registration;
using Microsoft.EntityFrameworkCore;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;
using HealthCheck.Registration;
using Shared.Messaging.Events;
using Shared.Messaging;
using System.Text.Json;

namespace NotificationInboxWorker;

public class Worker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<Worker> _logger;
    private readonly int _maxRetryCount;
    private readonly int _retentionDays;
    private readonly int _batchSize;
    private readonly TimeSpan _pollInterval;

    private readonly TimeSpan _cleanupInterval;

    private readonly WorkerHeartbeat _heartbeat;

    private DateTime _lastCleanupAt = DateTime.MinValue;

    public Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger, IConfiguration configuration,
        WorkerHeartbeat heartbeat)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _heartbeat = heartbeat;
        _maxRetryCount = configuration.GetValue("Inbox:MaxRetryCount", 5);
        _retentionDays = configuration.GetValue("Inbox:RetentionDays", 30);
        _batchSize = configuration.GetValue("Inbox:BatchSize", 50);
        _pollInterval = TimeSpan.FromSeconds(configuration.GetValue("Inbox:PollIntervalSeconds", 5));
        _cleanupInterval = TimeSpan.FromHours(configuration.GetValue("Inbox:CleanupIntervalHours", 1));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NotificationInboxWorker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessInboxMessagesAsync(stoppingToken);
                await CleanupProcessedMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in NotificationInboxWorker.");
            }

            // Tur tamamlandı. try/catch'in dışında: hata alan ama dönmeye devam eden
            // döngü canlıdır; asılı kalan döngü ise bu satıra hiç ulaşamaz.
            _heartbeat.Beat();

            await Task.Delay(_pollInterval, stoppingToken);
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
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0) return;

        _logger.LogInformation("Processing {Count} inbox messages.", messages.Count);

        foreach (var message in messages)
        {
            using var activity = AppTracing.StartLinked(
                "inbox.process", message.TraceParent, ActivityKind.Consumer);

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
                recipientKeycloakId: evt.KeycloakId,
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

            var authorAlreadyNotified = await db.Notifications.AnyAsync(
                n => n.ReferenceId == evt.ArticleId && n.Type == "article_published" && n.IsSent,
                cancellationToken);

            if (authorAlreadyNotified)
            {
                _logger.LogInformation(
                    "Article {ArticleId}: author already notified in an earlier attempt, skipping.", evt.ArticleId);
            }
            else
            {
                var contact = await identityContactClient.GetContactAsync(evt.AuthorKeycloakId, cancellationToken)
                    ?? throw new InvalidOperationException($"Yazarın iletişim bilgisi bulunamadı: {evt.AuthorKeycloakId}");

                await SendAndSaveAsync(emailService, db,
                    type: "article_published",
                    recipient: contact.Email,
                    recipientKeycloakId: contact.KeycloakId,
                    subject: $"Haberiniz Yayınlandı: {evt.Title}",
                    body: $"""
                        <html><body style="font-family:Arial,sans-serif;padding:20px">
                            <h2>Merhaba {contact.FirstName},</h2>
                            <p>"<strong>{evt.Title}</strong>" başlıklı haberiniz yayına alındı! 📰</p>
                            <p><strong>Yayın Tarihi:</strong> {ToTurkeyLocalTime(evt.PublishedAt):dd MMMM yyyy HH:mm}</p>
                            <br/><p><strong>Telgraf Ekibi</strong></p>
                        </body></html>
                        """,
                    cancellationToken,
                    referenceId: evt.ArticleId);
            }

            // Editör haberi oluştururken "abonelere bildir" işaretlediyse bültene
            // abone olan kullanıcılara da duyuru gider.
            if (evt.NotifySubscribers)
                await NotifySubscribersAsync(evt, emailService, identityContactClient, db, cancellationToken);
        }
        else
        {
            _logger.LogWarning("Unknown topic '{Topic}', skipping.", message.Topic);
        }
    }

    private async Task CleanupProcessedMessagesAsync(CancellationToken cancellationToken)
    {
        if (DateTime.UtcNow - _lastCleanupAt < _cleanupInterval)
            return;

        _lastCleanupAt = DateTime.UtcNow;

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InboxWorkerDbContext>();

        var cutoff = DateTime.UtcNow.AddDays(-_retentionDays);

        var deleted = await db.InboxMessages
            .Where(m => m.IsProcessed && m.ProcessedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
            _logger.LogInformation("Cleaned up {Count} processed inbox messages older than {Cutoff:u}.", deleted, cutoff);
    }

    private async Task NotifySubscribersAsync(
        ArticlePublishedEvent evt,
        IEmailService emailService,
        IIdentityContactClient identityContactClient,
        InboxWorkerDbContext db,
        CancellationToken cancellationToken)
    {
        var subscribers = await identityContactClient.GetSubscribersAsync(cancellationToken);
        if (subscribers.Count == 0)
        {
            _logger.LogInformation("Article {ArticleId} has notify flag but there are no subscribers.", evt.ArticleId);
            return;
        }

        var alreadyNotified = await db.Notifications
            .Where(n => n.ReferenceId == evt.ArticleId && n.Type == "article_broadcast" && n.IsSent)
            .Select(n => n.RecipientEmail)
            .ToListAsync(cancellationToken);

        var pending = subscribers.Where(s => !alreadyNotified.Contains(s.Email)).ToList();

        _logger.LogInformation(
            "Article {ArticleId}: notifying {Pending} of {Total} subscribers ({Skipped} already sent).",
            evt.ArticleId, pending.Count, subscribers.Count, subscribers.Count - pending.Count);

        foreach (var subscriber in pending)
        {
            await SendAndSaveAsync(emailService, db,
                type: "article_broadcast",
                recipient: subscriber.Email,
                recipientKeycloakId: subscriber.KeycloakId,
                subject: $"Yeni haber: {evt.Title}",
                body: $"""
                    <html><body style="font-family:Arial,sans-serif;padding:20px">
                        <h2>Merhaba {subscriber.FirstName},</h2>
                        <p>Telgraf'ta yeni bir haber yayınlandı:</p>
                        <p style="font-size:18px"><strong>{evt.Title}</strong></p>
                        <p><strong>Yayın Tarihi:</strong> {ToTurkeyLocalTime(evt.PublishedAt):dd MMMM yyyy HH:mm}</p>
                        <br/><p>İyi okumalar,</p><p><strong>Telgraf Ekibi</strong></p>
                        <hr/>
                        <p style="font-size:12px;color:#888">
                            Bu e-postayı bülten aboneliğiniz olduğu için aldınız.
                            Hesabım sayfasından aboneliğinizi kapatabilirsiniz.
                        </p>
                    </body></html>
                    """,
                cancellationToken,
                referenceId: evt.ArticleId);
        }
    }

    private static DateTime ToTurkeyLocalTime(DateTime utc) => utc.AddHours(3);

    private async Task SendAndSaveAsync(IEmailService emailService, InboxWorkerDbContext db,
        string type, string recipient, string recipientKeycloakId, string subject, string body,
        CancellationToken cancellationToken, Guid? referenceId = null)
    {
        var notification = new Notification
        {
            Type = type,
            RecipientEmail = recipient,
            Subject = subject,
            Body = body,
            ReferenceId = referenceId
        };

        await emailService.SendAsync(recipient, subject, body, cancellationToken);
        notification.IsSent = true;
        notification.SentAt = DateTime.UtcNow;

        db.Notifications.Add(notification);

        _logger.LogInformation("Mail gönderildi: alıcı {RecipientKeycloakId}, tür '{Type}'.",
            recipientKeycloakId, type);
    }
}
