using MediatR;
using NewsService.Application.Features.Queries.Tag.Response;

namespace NewsService.Application.Features.Queries.Tag.Request;

public class GetAllTagsQuery : IRequest<List<GetAllTagsResponse>>
{
}
