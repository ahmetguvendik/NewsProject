using IdentityService.Application.Features.Queries.User.Request;
using IdentityService.Application.Features.Queries.User.Response;
using IdentityService.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Application.Features.Handlers.User.QueryHandlers;

public class GetAllUsersQueryHandler : IRequestHandler<GetAllUsersQuery, List<GetAllUsersResponse>>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;

    public GetAllUsersQueryHandler(IGenericRepository<Domain.Entities.User> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<GetAllUsersResponse>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetQueryable()
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ToListAsync(cancellationToken);

        return users.Select(user => new GetAllUsersResponse
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl,
            IsActive = user.IsActive,
            Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList()
        }).ToList();
    }
}
