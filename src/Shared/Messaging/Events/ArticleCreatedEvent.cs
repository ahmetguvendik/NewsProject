namespace Shared.Messaging.Events;

public class ArticleCreatedEvent
{
    public Guid ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AuthorKeycloakId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
