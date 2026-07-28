using IdentityService.Application.Features.Queries.User.Request;
using IdentityService.Application.Features.Queries.User.Response;
using IdentityService.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Application.Features.Handlers.User.QueryHandlers;

public class GetUserDirectoryQueryHandler : IRequestHandler<GetUserDirectoryQuery, List<UserDirectoryEntryResponse>>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;

    public GetUserDirectoryQueryHandler(IGenericRepository<Domain.Entities.User> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<UserDirectoryEntryResponse>> Handle(GetUserDirectoryQuery request, CancellationToken cancellationToken)
    {
        if (request.KeycloakIds.Count == 0) return [];

        // Bulunamayan ID'ler (ör. silinmiş kullanıcı) sessizce atlanır — çağıran
        // taraf eksik girişleri kendi fallback'iyle (kısaltılmış ID vb.) gösterir.
        return await _userRepository.GetQueryable()
            .Where(u => request.KeycloakIds.Contains(u.KeycloakId))
            .Select(u => new UserDirectoryEntryResponse
            {
                KeycloakId = u.KeycloakId,
                DisplayName = (u.FirstName + " " + u.LastName).Trim()
            })
            .ToListAsync(cancellationToken);
    }
}
