using IdentityService.Application.Caching;
using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Features.Commands.User.Response;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using MediatR;
using Shared.Exceptions;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UpdateUserResponse>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public UpdateUserCommandHandler(
        IGenericRepository<Domain.Entities.User> userRepository,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<UpdateUserResponse> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id.ToString(), cancellationToken)
            ?? throw NotFoundException.User(request.Id);

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.AvatarUrl = request.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Ad-soyad hem dizinde hem profilde görünüyor. Dizinde tüm hash yerine
        // yalnızca bu kullanıcının alanı düşürülür — tamamını silmek, tek bir
        // isim değişikliği için bütün isabeti çöpe atmak olurdu.
        await _cache.RemoveHashFieldAsync(CacheKeys.Directory, user.KeycloakId, cancellationToken);
        await _cache.RemoveAsync(CacheKeys.Profile(user.KeycloakId), cancellationToken);

        return new UpdateUserResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            IsActive = user.IsActive
        };
    }
}
