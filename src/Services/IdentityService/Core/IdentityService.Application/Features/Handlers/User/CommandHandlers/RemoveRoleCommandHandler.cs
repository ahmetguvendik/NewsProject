using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using IdentityService.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

public class RemoveRoleCommandHandler : IRequestHandler<RemoveRoleCommand>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IKeycloakAdminClient _keycloakAdminClient;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveRoleCommandHandler(
        IGenericRepository<Domain.Entities.User> userRepository,
        IUserRoleRepository userRoleRepository,
        IKeycloakAdminClient keycloakAdminClient,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _keycloakAdminClient = keycloakAdminClient;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId.ToString(), cancellationToken)
            ?? throw NotFoundException.User(request.UserId);

        // Bilinmeyen rol adı → 400 ValidationException
        var roleId = RoleConstants.TryGetId(request.RoleName)
            ?? throw new ValidationException("roleName",
                $"'{request.RoleName}' geçerli bir rol değil. Geçerli roller: admin, editor, user.");

        // Kullanıcıda bu rol zaten yoksa kaldıracak bir şey yok → 404
        var existing = await _userRoleRepository.GetAsync(user.Id, roleId, cancellationToken)
            ?? throw NotFoundException.UserRole(request.UserId, request.RoleName);

        // Sistemde admin rolü hiç kalmasın diye son admin'in rolü kaldırılamaz
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

        // Keycloak'ta kaldır
        await _keycloakAdminClient.RemoveRoleAsync(user.KeycloakId, request.RoleName, cancellationToken);

        try
        {
            // DB'den de kaldır
            await _userRoleRepository.DeleteAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // DB başarısız olursa Keycloak'taki kaldırmayı geri al (compensating transaction) —
            // aksi halde Keycloak ve DB birbirinden sapar.
            await _keycloakAdminClient.AssignRoleAsync(user.KeycloakId, request.RoleName, cancellationToken);
            throw;
        }
    }
}
