using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using IdentityService.Domain.Constants;
using IdentityService.Domain.Entities;
using MediatR;
using Shared.Exceptions;
using Shared.Security;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

public class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IKeycloakAdminClient _keycloakAdminClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public AssignRoleCommandHandler(
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

    public async Task Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId.ToString(), cancellationToken)
            ?? throw NotFoundException.User(request.UserId);

        // Bilinmeyen rol adı → 400 ValidationException
        var roleId = RoleConstants.TryGetId(request.RoleName)
            ?? throw new ValidationException("roleName",
                $"'{request.RoleName}' geçerli bir rol değil. Geçerli roller: admin, editor, user.");

        var existing = await _userRoleRepository.GetAsync(user.Id, roleId, cancellationToken);
        if (existing is not null)
            throw ConflictException.RoleAlreadyAssigned(request.UserId, request.RoleName);

        // Keycloak'ta ata
        await _keycloakAdminClient.AssignRoleAsync(user.KeycloakId, request.RoleName, cancellationToken);

        // DB başarısız olursa Keycloak'taki atamayı geri al (compensating transaction)
        try
        {
            await _userRoleRepository.CreateAsync(
                new UserRole { UserId = user.Id, RoleId = roleId },
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Kaldırma ile SİMETRİK. Burada güvenlik sorunu yok — yeni rol
            // token'da olmadığı için kullanıcı fazladan bir şey yapamaz. Damga
            // yine de yazılıyor çünkü alternatifi sessiz bir tutarsızlık:
            // "seni admin yaptım" denen kullanıcı menüyü 1 saat boyunca
            // göremez ve neden olduğunu anlayamazdı. Bedeli bir kez tekrar
            // giriş yapmak; karşılığı yetkinin hemen geçerli olması.
            await _cache.SetAsync(
                TokenInvalidation.Key(user.KeycloakId),
                TokenInvalidation.Value(DateTimeOffset.UtcNow, TokenInvalidation.ReasonRoles),
                TokenInvalidation.Retention,
                cancellationToken);
        }
        catch
        {
            await _keycloakAdminClient.RemoveRoleAsync(user.KeycloakId, request.RoleName, cancellationToken);
            throw;
        }
    }
}
