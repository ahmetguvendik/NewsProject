using IdentityService.Application.Caching;
using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using MediatR;
using Shared.Exceptions;
using Shared.Security;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IKeycloakAdminClient _keycloakAdminClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ITokenRevocationStore _revocations;

    public DeactivateUserCommandHandler(
        IGenericRepository<Domain.Entities.User> userRepository,
        IKeycloakAdminClient keycloakAdminClient,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        ITokenRevocationStore revocations)
    {
        _userRepository = userRepository;
        _keycloakAdminClient = keycloakAdminClient;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _revocations = revocations;
    }

    public async Task Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId.ToString(), cancellationToken)
            ?? throw NotFoundException.User(request.UserId);

        if (!user.IsActive)
            return; // zaten pasif — idempotent

        await _keycloakAdminClient.DisableUserAsync(user.KeycloakId, cancellationToken);

        try
        {
            user.IsActive = false;
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Kayıttan önce: iptal kaydı yazılamazsa hesap da pasife alınmasın.
            await _revocations.RevokeAsync(user.KeycloakId, TokenRevocation.ReasonAccountDisabled, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // IsActive profilde dönüyor. Telafi bloğunun içinde: DB yazması
            // başarısız olursa zaten geri alınıyor ve düşürülecek bir şey yok.
            await _cache.RemoveAsync(CacheKeys.Profile(user.KeycloakId), cancellationToken);
        }
        catch
        {
            // DB başarısız olursa Keycloak'taki değişikliği geri al (compensating transaction)
            await _keycloakAdminClient.EnableUserAsync(user.KeycloakId, cancellationToken);
            throw;
        }
    }
}
