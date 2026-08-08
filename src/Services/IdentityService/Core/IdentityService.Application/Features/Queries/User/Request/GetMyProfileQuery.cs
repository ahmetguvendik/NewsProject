using IdentityService.Application.Features.Queries.User.Response;
using MediatR;

namespace IdentityService.Application.Features.Queries.User.Request;

/// <summary>KeycloakId token'dan gelir — kullanıcı yalnızca kendi profilini okuyabilir.</summary>
public class GetMyProfileQuery : IRequest<MyProfileResponse>
{
    public string KeycloakId { get; set; } = string.Empty;
}
