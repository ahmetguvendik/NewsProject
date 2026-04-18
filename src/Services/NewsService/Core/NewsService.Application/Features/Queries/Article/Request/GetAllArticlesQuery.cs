using MediatR;
using NewsService.Application.Features.Queries.Article.Response;

namespace NewsService.Application.Features.Queries.Article.Request;

public class GetAllArticlesQuery : IRequest<List<GetAllArticlesResponse>>
{
}
