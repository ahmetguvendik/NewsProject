namespace IdentityService.Application.Auditing;

public sealed class KeycloakSecurityEvent
{
    public long Time { get; init; }

    public string Type { get; init; } = string.Empty;

    public string? UserId { get; init; }

    public string? IpAddress { get; init; }

    public string? ClientId { get; init; }
}
