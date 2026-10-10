using IdentityService.Application.Caching;
using IdentityService.Application.Features.Queries.User.Response;
using MediatR;

namespace IdentityService.Application.Features.Queries.User.Request;

public class GetMyProfileQuery : IRequest<MyProfileResponse>, ICacheableQuery
{
    public string KeycloakId { get; set; } = string.Empty;

    public string CacheKey => CacheKeys.Profile(KeycloakId);

    public TimeSpan Duration => TimeSpan.FromMinutes(30);
}
