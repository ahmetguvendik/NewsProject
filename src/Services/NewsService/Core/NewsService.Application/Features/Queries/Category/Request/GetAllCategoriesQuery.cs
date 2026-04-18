using MediatR;
using NewsService.Application.Features.Queries.Category.Response;

namespace NewsService.Application.Features.Queries.Category.Request;

public class GetAllCategoriesQuery : IRequest<List<GetAllCategoriesResponse>>
{
}
