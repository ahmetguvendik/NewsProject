namespace NewsService.Application.Features.Queries.Article.Response;

public class GetAllArticlesResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string AuthorKeycloakId { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
