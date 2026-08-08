using IdentityService.Application.Features.Queries.User.Request;
using IdentityService.Application.Features.Queries.User.Response;
using IdentityService.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;

namespace IdentityService.Application.Features.Handlers.User.QueryHandlers;

public class GetMyProfileQueryHandler : IRequestHandler<GetMyProfileQuery, MyProfileResponse>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;

    public GetMyProfileQueryHandler(IGenericRepository<Domain.Entities.User> userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<MyProfileResponse> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetQueryable()
            .FirstOrDefaultAsync(u => u.KeycloakId == request.KeycloakId, cancellationToken)
            ?? throw new NotFoundException(
                ErrorCodes.User.NotFound,
                "Kullanıcı bulunamadı.",
                "Token geçerli ancak bu kullanıcı yerel veritabanında yok.");

        return new MyProfileResponse
        {
            Id = user.Id,
            KeycloakId = user.KeycloakId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsSubscribed = user.IsSubscribed
        };
    }
}
