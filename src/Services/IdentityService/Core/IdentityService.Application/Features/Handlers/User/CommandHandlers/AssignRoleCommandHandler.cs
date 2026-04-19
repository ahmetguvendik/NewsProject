using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using IdentityService.Domain.Constants;
using IdentityService.Domain.Entities;
using MediatR;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IKeycloakAdminClient _keycloakAdminClient;
    private readonly IUnitOfWork _unitOfWork;

    public AssignRoleCommandHandler(
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

    public async Task Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId.ToString(), cancellationToken)
            ?? throw new Exception($"User not found: {request.UserId}");

        // RoleConstants.GetId — bilinmeyen rol gelirse exception fırlatır
        var roleId = RoleConstants.GetId(request.RoleName);

        var existing = await _userRoleRepository.GetAsync(user.Id, roleId, cancellationToken);
        if (existing is not null)
            throw new Exception($"Role '{request.RoleName}' already assigned to this user.");

        // Keycloak'ta ata
        await _keycloakAdminClient.AssignRoleAsync(user.KeycloakId, request.RoleName, cancellationToken);

        // DB'de kaydet
        await _userRoleRepository.CreateAsync(
            new UserRole { UserId = user.Id, RoleId = roleId },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
