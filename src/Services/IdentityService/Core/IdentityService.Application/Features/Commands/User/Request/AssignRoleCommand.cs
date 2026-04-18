using MediatR;

namespace IdentityService.Application.Features.Commands.User.Request;

public class AssignRoleCommand : IRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}
