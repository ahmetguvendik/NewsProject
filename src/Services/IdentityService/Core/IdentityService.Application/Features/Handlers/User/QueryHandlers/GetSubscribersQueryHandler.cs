using IdentityService.Application.Features.Queries.User.Request;
using IdentityService.Application.Features.Queries.User.Response;
using IdentityService.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Application.Features.Handlers.User.QueryHandlers;

public class GetSubscribersQueryHandler : IRequestHandler<GetSubscribersQuery, List<UserContactResponse>>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;

    public GetSubscribersQueryHandler(IGenericRepository<Domain.Entities.User> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<UserContactResponse>> Handle(GetSubscribersQuery request, CancellationToken cancellationToken)
    {
        // Pasife alınmış kullanıcılara bildirim gitmemeli — GetQueryable zaten
        // silinmişleri eliyor, IsActive kontrolünü burada ekliyoruz.
        return await _userRepository.GetQueryable()
            .Where(u => u.IsSubscribed && u.IsActive)
            .Select(u => new UserContactResponse
            {
                KeycloakId = u.KeycloakId,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName
            })
            .ToListAsync(cancellationToken);
    }
}
