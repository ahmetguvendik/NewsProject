using IdentityService.Application.Interfaces;
using IdentityService.Infrastructure.Caching;
using IdentityService.Infrastructure.Keycloak;
using IdentityService.Infrastructure.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace IdentityService.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IKeycloakAdminClient, KeycloakAdminClient>();

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(configuration["Redis:Connection"] ?? "redis:6379");
            options.AbortOnConnectFail = false;
            options.ConnectRetry = 1;
            options.ConnectTimeout = 500;
            options.SyncTimeout = 500;
            options.AsyncTimeout = 500;
            return ConnectionMultiplexer.Connect(options);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();
        services.AddSingleton<ITokenRevocationStore, RedisTokenRevocationStore>();

        return services;
    }
}
