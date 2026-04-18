namespace NewsService.Application.Features.Commands.Article.Response;

public class UpdateArticleResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public Guid CategoryId { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
