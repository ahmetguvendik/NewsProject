namespace NewsService.Application.Features.Queries.Category.Response;

public class GetAllCategoriesResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ArticleCount { get; set; }
}
