using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using MediatR;
using Shared.Exceptions;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

public class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IKeycloakAdminClient _keycloakAdminClient;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateUserCommandHandler(
        IGenericRepository<Domain.Entities.User> userRepository,
        IKeycloakAdminClient keycloakAdminClient,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _keycloakAdminClient = keycloakAdminClient;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId.ToString(), cancellationToken)
            ?? throw NotFoundException.User(request.UserId);

        if (user.IsActive)
            return; // zaten aktif — idempotent

        await _keycloakAdminClient.EnableUserAsync(user.KeycloakId, cancellationToken);

        try
        {
            user.IsActive = true;
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // DB başarısız olursa Keycloak'taki değişikliği geri al (compensating transaction)
            await _keycloakAdminClient.DisableUserAsync(user.KeycloakId, cancellationToken);
            throw;
        }
    }
}
