using IdentityService.Application.Features.Queries.User.Response;
using MediatR;

namespace IdentityService.Application.Features.Queries.User.Request;

public class GetAllUsersQuery : IRequest<List<GetAllUsersResponse>>
{
}
