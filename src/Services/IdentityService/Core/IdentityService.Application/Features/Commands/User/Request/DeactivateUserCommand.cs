using MediatR;

namespace IdentityService.Application.Features.Commands.User.Request;

public class DeactivateUserCommand : IRequest
{
    public Guid UserId { get; set; }
}
