using System.Data;
using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using IdentityService.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using Shared.Security;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

public class RemoveRoleCommandHandler : IRequestHandler<RemoveRoleCommand>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IKeycloakAdminClient _keycloakAdminClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public RemoveRoleCommandHandler(
        IGenericRepository<Domain.Entities.User> userRepository,
        IUserRoleRepository userRoleRepository,
        IKeycloakAdminClient keycloakAdminClient,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _keycloakAdminClient = keycloakAdminClient;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId.ToString(), cancellationToken)
            ?? throw NotFoundException.User(request.UserId);

        // Bilinmeyen rol adı → 400 ValidationException
        var roleId = RoleConstants.TryGetId(request.RoleName)
            ?? throw new ValidationException("roleName",
                $"'{request.RoleName}' geçerli bir rol değil. Geçerli roller: admin, editor, user.");

        // Serializable: eşzamanlı iki istek "son admin" kuralını aşıp sistemi admin'siz bırakmasın.
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var removedInKeycloak = false;
        try
        {
            // Kullanıcıda bu rol zaten yoksa kaldıracak bir şey yok → 404
            var existing = await _userRoleRepository.GetAsync(user.Id, roleId, cancellationToken)
                ?? throw NotFoundException.UserRole(request.UserId, request.RoleName);

            if (roleId == RoleConstants.AdminId)
            {
                var adminCount = await _userRoleRepository.GetQueryable()
                    .CountAsync(ur => ur.RoleId == RoleConstants.AdminId, cancellationToken);

                if (adminCount <= 1)
                    throw new BusinessException(
                        ErrorCodes.User.LastAdminCannotBeRemoved,
                        "Son admin rolü kaldırılamaz.",
                        "Sistemde en az bir admin kalmalı. Önce başka bir kullanıcıya admin rolü atayın, sonra bunu kaldırın.");
            }

            await _keycloakAdminClient.RemoveRoleAsync(user.KeycloakId, request.RoleName, cancellationToken);
            removedInKeycloak = true;

            await _userRoleRepository.DeleteAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Serializable çakışması çoğunlukla burada çıkıyor: yarışı kaybeden
            // işlem commit anında iptal ediliyor.
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            // İstek iptal edilmiş olsa bile geri alma ve telafi çalışmalı.
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);

            // Keycloak'a henüz dokunulmadıysa (404, son admin kuralı, ya da
            // Keycloak çağrısının kendisi başarısız) geri alınacak bir şey yok.
            if (removedInKeycloak)
                await _keycloakAdminClient.AssignRoleAsync(user.KeycloakId, request.RoleName, CancellationToken.None);

            throw;
        }

        // Commit'ten sonra: işlem iptal olursa kullanıcı boşuna oturumdan atılmasın.
        await _cache.SetAsync(
            TokenRevocation.Key(user.KeycloakId),
            TokenRevocation.Now(TokenRevocation.ReasonRoleChange),
            TokenRevocation.Retention,
            cancellationToken);
    }
}
