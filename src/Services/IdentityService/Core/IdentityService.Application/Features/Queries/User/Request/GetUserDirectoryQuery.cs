using IdentityService.Application.Features.Queries.User.Response;
using MediatR;

namespace IdentityService.Application.Features.Queries.User.Request;

public class GetUserDirectoryQuery : IRequest<List<UserDirectoryEntryResponse>>
{
    public List<string> KeycloakIds { get; set; } = [];
}
