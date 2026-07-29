using IdentityService.Application.Features.Queries.User.Response;
using MediatR;

namespace IdentityService.Application.Features.Queries.User.Request;

/// <summary>
/// Yalnızca servisler arası dahili çağrılar için — e-posta içerdiği için
/// [AllowAnonymous] bir uçtan asla dönmemeli (bkz. GetUserDirectoryQuery).
/// </summary>
public class GetUserContactQuery : IRequest<UserContactResponse?>
{
    public string KeycloakId { get; set; } = string.Empty;
}
