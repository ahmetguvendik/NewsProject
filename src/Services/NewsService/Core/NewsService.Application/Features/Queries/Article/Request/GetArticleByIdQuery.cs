using MediatR;
using NewsService.Application.Features.Queries.Article.Response;

namespace NewsService.Application.Features.Queries.Article.Request;

public class GetArticleByIdQuery : IRequest<GetArticleByIdResponse>
{
    public Guid Id { get; set; }
}
