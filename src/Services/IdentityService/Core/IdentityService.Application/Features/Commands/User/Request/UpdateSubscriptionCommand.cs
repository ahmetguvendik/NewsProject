using System.Text.Json.Serialization;
using MediatR;

namespace IdentityService.Application.Features.Commands.User.Request;

public class UpdateSubscriptionCommand : IRequest
{
    public bool IsSubscribed { get; set; }

    // Body'den gelmez — controller JWT'den set eder, kullanıcı yalnızca
    // kendi aboneliğini değiştirebilir.
    [JsonIgnore]
    public string KeycloakId { get; set; } = string.Empty;
}
