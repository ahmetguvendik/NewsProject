using NotificationService.Domain.Common;

namespace NotificationService.Domain.Entities;

public class Notification : BaseEntity
{
    public string Type { get; set; } = string.Empty;         // welcome, article_published, article_broadcast
    public string RecipientEmail { get; set; } = string.Empty;

    /// <summary>
    /// İlgili kaydın kimliği (ör. ArticleId). Bir haber çok sayıda aboneye
    /// gönderilirken, mesaj yarıda hata alıp yeniden denenirse zaten mail
    /// gitmiş kişilere tekrar gönderilmesini engellemek için kullanılıyor.
    /// </summary>
    public Guid? ReferenceId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
}
