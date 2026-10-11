using System.Data;
using IdentityService.Application.Exceptions;
using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using IdentityService.Domain.Constants;
using IdentityService.Domain.Entities;
using MediatR;
using Shared.Exceptions;
using Shared.Security;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

public class UpdateUserRoleCommandHandler : IRequestHandler<UpdateUserRoleCommand>
{
    // "user" rolü herkeste kalır; bunların en fazla biri eklenir.
    private static readonly string[] PrivilegedRoles = [RoleConstants.Editor, RoleConstants.Admin];

    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IKeycloakAdminClient _keycloakAdminClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenRevocationStore _revocations;

    public UpdateUserRoleCommandHandler(
        IGenericRepository<Domain.Entities.User> userRepository,
        IUserRoleRepository userRoleRepository,
        IKeycloakAdminClient keycloakAdminClient,
        IUnitOfWork unitOfWork,
        ITokenRevocationStore revocations)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _keycloakAdminClient = keycloakAdminClient;
        _unitOfWork = unitOfWork;
        _revocations = revocations;
    }

    public async Task Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId.ToString(), cancellationToken)
            ?? throw NotFoundException.User(request.UserId);

        var target = request.RoleName.ToLowerInvariant();

        // Serializable: eşzamanlı iki istek "son admin" kuralını aşıp sistemi admin'siz bırakmasın.
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        // Keycloak'ta yapılanlar; hata olursa tersten geri alınır.
        var applied = new List<(string Role, bool Added)>();
        try
        {
            var current = (await _userRoleRepository.GetByUserIdAsync(user.Id, cancellationToken))
                .ToDictionary(ur => ur.RoleId);

            var changes = PrivilegedRoles
                .Select(role => (Role: role, Add: role == target))
                .Where(c => current.ContainsKey(RoleConstants.GetId(c.Role)) != c.Add)
                .ToList();

            if (changes.Count == 0)
            {
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return; // zaten bu yetkide — idempotent
            }

            // Pasif bir adminin yetkisi her zaman düşürülebilir: aktif admin sayısını azaltmaz.
            if (changes.Any(c => c.Role == RoleConstants.Admin && !c.Add) && user.IsActive
                && await _userRoleRepository.CountActiveAdminsAsync(cancellationToken) <= 1)
                throw BusinessException.LastAdminRoleRemoval();

            foreach (var (role, add) in changes)
            {
                if (add)
                {
                    await _keycloakAdminClient.AssignRoleAsync(user.KeycloakId, role, cancellationToken);
                    applied.Add((role, true));
                    await _userRoleRepository.CreateAsync(
                        new UserRole { UserId = user.Id, RoleId = RoleConstants.GetId(role) }, cancellationToken);
                }
                else
                {
                    await _keycloakAdminClient.RemoveRoleAsync(user.KeycloakId, role, cancellationToken);
                    applied.Add((role, false));
                    await _userRoleRepository.DeleteAsync(current[RoleConstants.GetId(role)], cancellationToken);
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _revocations.RevokeAsync(user.KeycloakId, TokenRevocation.ReasonRoleChange, cancellationToken);

            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            // İstek iptal edilmiş olsa bile geri alma ve telafi çalışmalı.
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);

            for (var i = applied.Count - 1; i >= 0; i--)
            {
                var (role, added) = applied[i];
                if (added)
                    await _keycloakAdminClient.RemoveRoleAsync(user.KeycloakId, role, CancellationToken.None);
                else
                    await _keycloakAdminClient.AssignRoleAsync(user.KeycloakId, role, CancellationToken.None);
            }

            throw;
        }
    }
}
