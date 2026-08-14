using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;
using NewsService.Persistance.Contexts;
using NewsService.Persistance.Messaging;
using NewsService.Persistance.Repositories;
using NewsService.Persistance.Storage;
using NewsService.Persistance.UnitOfWorks;

namespace NewsService.Persistance;

public static class ServiceRegistration
{
    public static IServiceCollection AddPersistanceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NewsServiceDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IArticleTagRepository, ArticleTagRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEventPublisher, OutboxEventPublisher>();

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        // S3 client'ları pahalı ve thread-safe — istek başına yeniden kurulmamalı.
        services.AddSingleton<IStorageService, S3StorageService>();

        return services;
    }
}
