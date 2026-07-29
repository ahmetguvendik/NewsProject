using IdentityService.Application.Features.Queries.User.Request;
using IdentityService.Application.Features.Queries.User.Response;
using IdentityService.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Application.Features.Handlers.User.QueryHandlers;

public class GetUserContactQueryHandler : IRequestHandler<GetUserContactQuery, UserContactResponse?>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;

    public GetUserContactQueryHandler(IGenericRepository<Domain.Entities.User> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserContactResponse?> Handle(GetUserContactQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetQueryable()
            .FirstOrDefaultAsync(u => u.KeycloakId == request.KeycloakId, cancellationToken);

        if (user is null) return null;

        return new UserContactResponse
        {
            KeycloakId = user.KeycloakId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }
}
