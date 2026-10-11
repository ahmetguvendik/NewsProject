using System.Text.Json.Serialization;
using MediatR;

namespace IdentityService.Application.Features.Commands.User.Request;

// Yetki tek kademe: user (okuyucu), editor ya da admin.
public class UpdateUserRoleCommand : IRequest
{
    // Body'den gelmez — controller route'tan set eder.
    [JsonIgnore]
    public Guid UserId { get; set; }

    public string RoleName { get; set; } = string.Empty;
}
