using NewsService.Domain.Common;
namespace NewsService.Domain.Entities;

public class Article : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? ImageUrl { get; set; }
    public string AuthorKeycloakId { get; set; } = string.Empty;

    /// <summary>
    /// Editör haberi oluştururken işaretler. Haber YAYINLANDIĞINDA bültene
    /// abone kullanıcılara bildirim gönderilip gönderilmeyeceğini belirler.
    /// </summary>
    public bool NotifySubscribers { get; set; }

    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public ICollection<ArticleTag> ArticleTags { get; set; } = [];
}
