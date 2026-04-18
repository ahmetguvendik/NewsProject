using MediatR;

namespace IdentityService.Application.Features.Commands.User.Request;

public class DeleteUserCommand : IRequest
{
    public Guid Id { get; set; }
}
