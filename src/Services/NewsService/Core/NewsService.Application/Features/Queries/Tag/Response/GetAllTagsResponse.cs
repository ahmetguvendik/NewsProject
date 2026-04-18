namespace NewsService.Application.Features.Queries.Tag.Response;

public class GetAllTagsResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ArticleCount { get; set; }
}
