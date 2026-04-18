namespace NewsService.Application.Features.Commands.Tag.Response;

public class CreateTagResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
