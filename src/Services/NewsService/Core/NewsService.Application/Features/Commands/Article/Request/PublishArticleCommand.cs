using MediatR;

namespace NewsService.Application.Features.Commands.Article.Request;

public class PublishArticleCommand : IRequest
{
    public Guid Id { get; set; }
}
