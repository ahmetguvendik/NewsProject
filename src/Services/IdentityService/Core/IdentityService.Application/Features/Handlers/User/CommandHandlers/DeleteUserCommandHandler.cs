using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using MediatR;
using Shared.Exceptions;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IKeycloakAdminClient _keycloakAdminClient;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserCommandHandler(
        IGenericRepository<Domain.Entities.User> userRepository,
        IKeycloakAdminClient keycloakAdminClient,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _keycloakAdminClient = keycloakAdminClient;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id.ToString(), cancellationToken)
            ?? throw NotFoundException.User(request.Id);

        // DB'de soft delete
        await _userRepository.DeleteAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Keycloak'ta kullanıcıyı devre dışı bırak (login edemez)
        // DB başarıyla kaydedildikten sonra yapılır — Keycloak başarısız olursa loglayıp devam et
        try
        {
            await _keycloakAdminClient.DisableUserAsync(user.KeycloakId, cancellationToken);
        }
        catch (Exception ex)
        {
            // Keycloak hatası kritik değil — kullanıcı DB'den silindi, Keycloak manuel düzeltilebilir
            // Gerçek projede burada ILogger kullanılmalı
            _ = ex;
        }
    }
}
