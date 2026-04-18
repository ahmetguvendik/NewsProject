using MediatR;
using NewsService.Application.Features.Queries.Tag.Response;

namespace NewsService.Application.Features.Queries.Tag.Request;

public class GetTagByIdQuery : IRequest<GetTagByIdResponse?>
{
    public Guid Id { get; set; }
}
