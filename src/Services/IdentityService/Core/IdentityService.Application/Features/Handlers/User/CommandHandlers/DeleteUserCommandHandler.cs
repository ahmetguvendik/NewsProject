using IdentityService.Application.Caching;
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
    private readonly ICacheService _cache;

    public DeleteUserCommandHandler(
        IGenericRepository<Domain.Entities.User> userRepository,
        IKeycloakAdminClient keycloakAdminClient,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _userRepository = userRepository;
        _keycloakAdminClient = keycloakAdminClient;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id.ToString(), cancellationToken)
            ?? throw NotFoundException.User(request.Id);

        // DB'de soft delete
        await _userRepository.DeleteAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Silinen kullanıcı dizinde kalırsa adı görünmeye devam eder.
        await _cache.RemoveHashFieldAsync(CacheKeys.Directory, user.KeycloakId, cancellationToken);
        await _cache.RemoveAsync(CacheKeys.Profile(user.KeycloakId), cancellationToken);

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
