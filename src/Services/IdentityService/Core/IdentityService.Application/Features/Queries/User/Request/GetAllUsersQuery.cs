using IdentityService.Application.Features.Queries.User.Response;
using MediatR;
using Shared.Models;

namespace IdentityService.Application.Features.Queries.User.Request;

public class GetAllUsersQuery : IRequest<PagedResult<GetAllUsersResponse>>
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
