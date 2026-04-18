using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using IdentityService.Persistance.Contexts;
using IdentityService.Persistance.Repositories;
using IdentityService.Persistance.UnitOfWorks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}
