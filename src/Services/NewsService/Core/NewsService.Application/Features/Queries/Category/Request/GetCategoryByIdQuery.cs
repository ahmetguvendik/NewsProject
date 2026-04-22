using MediatR;
using NewsService.Application.Features.Queries.Category.Response;

namespace NewsService.Application.Features.Queries.Category.Request;

public class GetCategoryByIdQuery : IRequest<GetCategoryByIdResponse>
{
    public Guid Id { get; set; }
}
