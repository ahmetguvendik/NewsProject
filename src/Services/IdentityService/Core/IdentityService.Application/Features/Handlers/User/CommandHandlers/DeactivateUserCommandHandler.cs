using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using MediatR;
using Shared.Exceptions;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

/// <summary>
/// Delete'ten farklı: kullanıcı listede görünmeye devam eder (soft-delete değil),
/// yalnızca IsActive=false olur ve Keycloak'ta login edemez hale gelir. Reversible —
/// bkz. ActivateUserCommandHandler.
/// </summary>
public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IKeycloakAdminClient _keycloakAdminClient;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateUserCommandHandler(
        IGenericRepository<Domain.Entities.User> userRepository,
        IKeycloakAdminClient keycloakAdminClient,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _keycloakAdminClient = keycloakAdminClient;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId.ToString(), cancellationToken)
            ?? throw NotFoundException.User(request.UserId);

        if (!user.IsActive)
            return; // zaten pasif — idempotent

        // Keycloak'ta önce devre dışı bırak — login engeli asıl burada oluşuyor.
        // DB güncellemesi Keycloak hatasını sessizce yutmuyor (Delete'in aksine):
        // bu komutun tek amacı login'i engellemek, o yüzden yarım kalırsa hata dönmeli.
        await _keycloakAdminClient.DisableUserAsync(user.KeycloakId, cancellationToken);

        try
        {
            user.IsActive = false;
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // DB başarısız olursa Keycloak'taki değişikliği geri al (compensating transaction)
            await _keycloakAdminClient.EnableUserAsync(user.KeycloakId, cancellationToken);
            throw;
        }
    }
}
