using IdentityService.Application.Features.Queries.User.Response;
using MediatR;

namespace IdentityService.Application.Features.Queries.User.Request;

/// <summary>
/// Verilen Keycloak ID'lerine karşılık gelen görünen adları döner.
/// Diğer servislerin (NewsService gibi) elinde yalnızca AuthorKeycloakId
/// bulunduğunda, ismi göstermek için bu sorguyu kullanır.
/// </summary>
public class GetUserDirectoryQuery : IRequest<List<UserDirectoryEntryResponse>>
{
    public List<string> KeycloakIds { get; set; } = [];
}
