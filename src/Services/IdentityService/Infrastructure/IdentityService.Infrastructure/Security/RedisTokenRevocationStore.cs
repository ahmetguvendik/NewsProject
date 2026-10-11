using System.Text.Json;
using IdentityService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Shared.Security;
using StackExchange.Redis;

namespace IdentityService.Infrastructure.Security;

public sealed class RedisTokenRevocationStore : ITokenRevocationStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisTokenRevocationStore> _logger;

    public RedisTokenRevocationStore(IConnectionMultiplexer redis, ILogger<RedisTokenRevocationStore> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task RevokeAsync(string keycloakId, string reason, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Serialize(TokenRevocation.Now(reason), TokenRevocation.SerializerOptions);

        try
        {
            await _redis.GetDatabase().StringSetAsync(TokenRevocation.Key(keycloakId), payload, TokenRevocation.Retention);
        }
        catch (Exception ex) when (ex is RedisException or TimeoutException)
        {
            _logger.LogError(ex, "Token iptal kaydı yazılamadı; eski token süresi dolana kadar geçerli kalacak. KullanıcıId={KeycloakId}", keycloakId);
        }
    }
}
