using IdentityService.Application.Features.Commands.User.Response;
using MediatR;

namespace IdentityService.Application.Features.Commands.User.Request;

public class CreateUserCommand : IRequest<CreateUserResponse>
{
    public string KeycloakId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}
