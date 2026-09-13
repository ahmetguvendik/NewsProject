using IdentityService.Application.Caching;
using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using MediatR;
using Shared.Exceptions;
using Shared.Security;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

public class ActivateUserCommandHandler : IRequestHandler<ActivateUserCommand>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IKeycloakAdminClient _keycloakAdminClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public ActivateUserCommandHandler(
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

    public async Task Handle(ActivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId.ToString(), cancellationToken)
            ?? throw NotFoundException.User(request.UserId);

        // Keycloak HER ZAMAN etkinleştiriliyor, IsActive'e bakılmadan.
        //
        // Önce "zaten aktif" ise hemen dönülüyordu. Bu, veritabanı ile Keycloak
        // ayrıştığında kullanıcıyı kurtarılamaz hale getiriyordu: IsActive=true
        // ama Keycloak devre dışıysa, "Aktif et" hiçbir şey yapmadan dönüyor ve
        // giriş engeli kalkmıyordu. Ayrışma normal akışta oluşmamalı ama Keycloak
        // elle de yönetilebiliyor.
        //
        // Çağrı idempotent: zaten etkin bir kullanıcıyı etkinleştirmek zararsız.
        await _keycloakAdminClient.EnableUserAsync(user.KeycloakId, cancellationToken);

        if (user.IsActive)
            return; // DB zaten güncel — gereksiz yazma ve önbellek düşürme atlanıyor

        try
        {
            user.IsActive = true;
            await _userRepository.UpdateAsync(user, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // IsActive profilde dönüyor. Telafi bloğunun içinde: DB yazması
            // başarısız olursa zaten geri alınıyor ve düşürülecek bir şey yok.
            await _cache.RemoveAsync(CacheKeys.Profile(user.KeycloakId), cancellationToken);

            // Pasif listesinden çıkar; aksi halde kullanıcı yeniden aktif edilse
            // bile kaydın TTL'i dolana kadar admin uçlarından reddedilmeye
            // devam ederdi.
            await _cache.RemoveAsync(DisabledUsers.Key(user.KeycloakId), cancellationToken);
        }
        catch
        {
            // DB başarısız olursa Keycloak'taki değişikliği geri al (compensating transaction)
            await _keycloakAdminClient.DisableUserAsync(user.KeycloakId, cancellationToken);
            throw;
        }
    }
}
