namespace Shared.Messaging.Events;

public class ArticlePublishedEvent
{
    public Guid ArticleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string AuthorKeycloakId { get; set; } = string.Empty;
    public DateTime PublishedAt { get; set; }
}
