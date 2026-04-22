using IdentityService.Application.Features.Queries.User.Response;
using MediatR;

namespace IdentityService.Application.Features.Queries.User.Request;

public class GetUserByIdQuery : IRequest<GetUserByIdResponse>
{
    public Guid Id { get; set; }
}
