namespace NewsService.Application.Features.Queries.Article.Response;

public class GetArticleByIdResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? ImageUrl { get; set; }

    // Ham değer: düzenleme formu bunu geri gönderir, yoksa anahtar tam URL'e dönüşür.
    public string? ImageKey { get; set; }
    public string? ImageSrcset { get; set; }

    public string AuthorKeycloakId { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<string> Tags { get; set; } = [];

    public List<Guid> TagIds { get; set; } = [];
}
