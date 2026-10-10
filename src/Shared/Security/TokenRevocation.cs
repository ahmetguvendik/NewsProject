using System.Text.Json;

namespace Shared.Security;

public sealed record TokenRevocation(DateTimeOffset RevokedAt, string Reason)
{
    public const string ReasonAccountDisabled = "account-disabled";

    public const string ReasonRoleChange = "role-change";

    public static string Key(string keycloakId) => $"identity:revoked:{keycloakId}";

    public static readonly TimeSpan Retention = TimeSpan.FromHours(2);

    public static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static TokenRevocation Now(string reason) =>
        new(DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds()), reason);

    public static TokenRevocation? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        try
        {
            var revocation = JsonSerializer.Deserialize<TokenRevocation>(raw, SerializerOptions);
            return string.IsNullOrEmpty(revocation?.Reason) ? null : revocation;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public bool Covers(long tokenIssuedAtUnix) => tokenIssuedAtUnix <= RevokedAt.ToUnixTimeSeconds();
}
