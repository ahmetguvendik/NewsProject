namespace IdentityService.Application.Caching;

public static class CacheKeys
{
    public const string Directory = "identity:directory";

    public static string Profile(string keycloakId) => $"identity:profile:{keycloakId}";
}
