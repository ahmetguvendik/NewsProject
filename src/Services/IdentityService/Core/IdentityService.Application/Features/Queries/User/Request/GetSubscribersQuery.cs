using IdentityService.Application.Features.Queries.User.Response;
using MediatR;

namespace IdentityService.Application.Features.Queries.User.Request;

/// <summary>
/// Bültene abone, aktif kullanıcıların iletişim bilgileri.
/// E-posta içerdiği için yalnızca Internal:ApiKey ile korunan uçtan dönmeli.
/// </summary>
public class GetSubscribersQuery : IRequest<List<UserContactResponse>>
{
}
