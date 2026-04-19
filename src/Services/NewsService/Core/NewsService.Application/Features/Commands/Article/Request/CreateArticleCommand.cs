using System.Text.Json.Serialization;
using MediatR;
using NewsService.Application.Features.Commands.Article.Response;

namespace NewsService.Application.Features.Commands.Article.Request;

public class CreateArticleCommand : IRequest<CreateArticleResponse>
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
    public List<Guid> TagIds { get; set; } = [];

    // Body'den gelmez — controller JWT'den set eder
    [JsonIgnore]
    public string AuthorKeycloakId { get; set; } = string.Empty;
}
