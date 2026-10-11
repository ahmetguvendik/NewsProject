using MediatR;

namespace IdentityService.Application.Features.Commands.User.Request;

public class DeactivateUserCommand : IRequest
{
    public Guid UserId { get; set; }

    // İşlemi yapan admin; controller JWT'den set eder.
    public string ActorKeycloakId { get; set; } = string.Empty;
}
