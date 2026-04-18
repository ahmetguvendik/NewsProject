namespace NewsService.Application.Features.Commands.Article.Response;

public class CreateArticleResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string AuthorKeycloakId { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
}
