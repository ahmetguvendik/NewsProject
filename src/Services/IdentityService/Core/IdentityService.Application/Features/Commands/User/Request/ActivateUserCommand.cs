using MediatR;

namespace IdentityService.Application.Features.Commands.User.Request;

public class ActivateUserCommand : IRequest
{
    public Guid UserId { get; set; }
}
