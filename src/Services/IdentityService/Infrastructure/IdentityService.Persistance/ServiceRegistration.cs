using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using IdentityService.Persistance.Caching;
using IdentityService.Persistance.Contexts;
using IdentityService.Persistance.Keycloak;
using IdentityService.Persistance.Messaging;
using IdentityService.Persistance.Repositories;
using IdentityService.Persistance.Security;
using IdentityService.Persistance.UnitOfWorks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace IdentityService.Persistance;

public static class ServiceRegistration
{
    public static IServiceCollection AddPersistanceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityServiceDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IUserRoleRepository, UserRoleRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();
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
