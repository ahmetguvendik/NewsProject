using IdentityService.Application.Caching;
using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

public class UpdateSubscriptionCommandHandler : IRequestHandler<UpdateSubscriptionCommand>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public UpdateSubscriptionCommandHandler(
        IGenericRepository<Domain.Entities.User> userRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task Handle(UpdateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetQueryable()
            .FirstOrDefaultAsync(u => u.KeycloakId == request.KeycloakId, cancellationToken)
            ?? throw new NotFoundException(
                ErrorCodes.User.NotFound,
                "Kullanıcı bulunamadı.",
                "Token geçerli ancak bu kullanıcı yerel veritabanında yok.");

        if (user.IsSubscribed == request.IsSubscribed)
            return; // değişiklik yok — idempotent

        user.IsSubscribed = request.IsSubscribed;
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Abonelik durumu profilde dönüyor; düşürülmezse kullanıcı az önce
        // yaptığı değişikliği kendi hesap sayfasında göremez.
        await _cache.RemoveAsync(CacheKeys.Profile(user.KeycloakId), cancellationToken);
    }
}
