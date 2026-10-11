using System.Data;
using IdentityService.Application.Caching;
using IdentityService.Application.Exceptions;
using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using IdentityService.Domain.Constants;
using MediatR;
using Shared.Exceptions;
using Shared.Security;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

public class DeactivateUserCommandHandler : IRequestHandler<DeactivateUserCommand>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IKeycloakAdminClient _keycloakAdminClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ITokenRevocationStore _revocations;

    public DeactivateUserCommandHandler(
        IGenericRepository<Domain.Entities.User> userRepository,
        IUserRoleRepository userRoleRepository,
        IKeycloakAdminClient keycloakAdminClient,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        ITokenRevocationStore revocations)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
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

        if (user.KeycloakId == request.ActorKeycloakId)
            throw BusinessException.SelfDeactivation();

        // Serializable: iki admin aynı anda birbirini pasife alıp sistemi admin'siz bırakmasın.
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var disabledInKeycloak = false;
        try
        {
            var isAdmin = await _userRoleRepository.GetAsync(user.Id, RoleConstants.AdminId, cancellationToken) is not null;
            if (isAdmin && await _userRoleRepository.CountActiveAdminsAsync(cancellationToken) <= 1)
                throw BusinessException.LastAdminDeactivation();

            await _keycloakAdminClient.DisableUserAsync(user.KeycloakId, cancellationToken);
            disabledInKeycloak = true;

            user.IsActive = false;
            await _userRepository.UpdateAsync(user, cancellationToken);

            await _revocations.RevokeAsync(user.KeycloakId, TokenRevocation.ReasonAccountDisabled, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            // İstek iptal edilmiş olsa bile geri alma ve telafi çalışmalı.
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);

            if (disabledInKeycloak)
                await _keycloakAdminClient.EnableUserAsync(user.KeycloakId, CancellationToken.None);

            throw;
        }

        // IsActive profilde dönüyor.
        await _cache.RemoveAsync(CacheKeys.Profile(user.KeycloakId), cancellationToken);
    }
}
