using IdentityService.Application.Features.Queries.User.Response;
using MediatR;

namespace IdentityService.Application.Features.Queries.User.Request;

public class GetUserContactQuery : IRequest<UserContactResponse?>
{
    public string KeycloakId { get; set; } = string.Empty;
}
