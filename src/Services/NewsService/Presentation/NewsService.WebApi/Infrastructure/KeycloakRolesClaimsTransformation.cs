using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace NewsService.WebApi.Infrastructure;

public class KeycloakRolesClaimsTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = (ClaimsIdentity)principal.Identity!;

        var realmAccessClaim = identity.FindFirst("realm_access");
        if (realmAccessClaim is not null)
        {
            var realmAccess = JsonDocument.Parse(realmAccessClaim.Value);
            if (realmAccess.RootElement.TryGetProperty("roles", out var roles))
            {
                foreach (var role in roles.EnumerateArray())
                {
                    var roleName = role.GetString();
                    if (roleName is not null && !identity.HasClaim(ClaimTypes.Role, roleName))
                        identity.AddClaim(new Claim(ClaimTypes.Role, roleName));
                }
            }
        }

        return Task.FromResult(principal);
    }
}
