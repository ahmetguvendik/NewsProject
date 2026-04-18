using MediatR;

namespace NewsService.Application.Features.Commands.Article.Request;

public class DeleteArticleCommand : IRequest
{
    public Guid Id { get; set; }
}
