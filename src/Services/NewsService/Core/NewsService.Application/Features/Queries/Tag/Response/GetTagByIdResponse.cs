namespace NewsService.Application.Features.Queries.Tag.Response;

public class GetTagByIdResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
