using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using IdentityService.Domain.Entities;
using MediatR;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IGenericRepository<Role> _roleRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AssignRoleCommandHandler(
        IGenericRepository<Domain.Entities.User> userRepository,
        IGenericRepository<Role> roleRepository,
        IUserRoleRepository userRoleRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _userRoleRepository = userRoleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId.ToString(), cancellationToken)
            ?? throw new Exception($"User not found: {request.UserId}");

        var role = await _roleRepository.GetByIdAsync(request.RoleId.ToString(), cancellationToken)
            ?? throw new Exception($"Role not found: {request.RoleId}");

        var existing = await _userRoleRepository.GetAsync(user.Id, role.Id, cancellationToken);
        if (existing is not null)
            throw new Exception("Role already assigned to this user.");

        await _userRoleRepository.CreateAsync(new UserRole { UserId = user.Id, RoleId = role.Id }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
