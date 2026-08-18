namespace NewsService.Application.Features.Queries.Article.Response;

public class GetAllArticlesResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }

    /// <summary>Liste kartlarının kapak görseli. Yoksa client kategoriden gradyan üretir.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Tarayıcının ekran boyutuna göre seçmesi için boy listesi; dış adreslerde null.</summary>
    public string? ImageSrcset { get; set; }

    public string AuthorKeycloakId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
