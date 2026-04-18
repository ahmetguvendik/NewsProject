using IdentityService.Application.Features.Queries.User.Request;
using IdentityService.Application.Features.Queries.User.Response;
using IdentityService.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Application.Features.Handlers.User.QueryHandlers;

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, GetUserByIdResponse?>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;

    public GetUserByIdQueryHandler(IGenericRepository<Domain.Entities.User> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<GetUserByIdResponse?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetQueryable()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user is null) return null;

        return new GetUserByIdResponse
        {
            Id = user.Id,
            KeycloakId = user.KeycloakId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            IsActive = user.IsActive,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        };
    }
}
