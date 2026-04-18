using IdentityService.Application.Features.Commands.User.Response;
using MediatR;

namespace IdentityService.Application.Features.Commands.User.Request;

public class UpdateUserCommand : IRequest<UpdateUserResponse>
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}
