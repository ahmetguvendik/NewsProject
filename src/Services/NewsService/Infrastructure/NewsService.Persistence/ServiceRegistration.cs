using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;
using NewsService.Persistence.Auditing;
using NewsService.Persistence.Contexts;
using NewsService.Persistence.Messaging;
using NewsService.Persistence.Repositories;
using NewsService.Persistence.UnitOfWorks;

namespace NewsService.Persistence;

public static class ServiceRegistration
{
    public static IServiceCollection AddPersistenceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ChangeAuditInterceptor>();

        services.AddDbContext<NewsServiceDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
                .AddInterceptors(serviceProvider.GetRequiredService<ChangeAuditInterceptor>()));

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IArticleTagRepository, ArticleTagRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();

        return services;
    }
}
