using NotificationService.Domain.Common;

namespace NotificationService.Domain.Entities;

public class Notification : BaseEntity
{
    public string Type { get; set; } = string.Empty;         // welcome, article_published
    public string RecipientEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
    public string? ErrorMessage { get; set; }
}
