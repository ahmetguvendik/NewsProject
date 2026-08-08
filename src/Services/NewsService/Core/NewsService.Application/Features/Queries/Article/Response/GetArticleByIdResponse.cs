namespace NewsService.Application.Features.Queries.Article.Response;

public class GetArticleByIdResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? ImageUrl { get; set; }
    public string AuthorKeycloakId { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    /// <summary>Okuma ekranında gösterilen etiket adları.</summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>Düzenleme formunun mevcut etiketleri işaretleyebilmesi için ID'ler.</summary>
    public List<Guid> TagIds { get; set; } = [];
}
