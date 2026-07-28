using MediatR;

namespace IdentityService.Application.Features.Commands.User.Request;

public class RemoveRoleCommand : IRequest
{
    public Guid UserId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}
