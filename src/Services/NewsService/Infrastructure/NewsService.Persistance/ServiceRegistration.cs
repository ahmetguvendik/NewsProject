using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;
using NewsService.Persistance.Auditing;
using NewsService.Persistance.Caching;
using NewsService.Persistance.Contexts;
using NewsService.Persistance.External;
using NewsService.Persistance.Messaging;
using NewsService.Persistance.Repositories;
using NewsService.Persistance.Storage;
using NewsService.Persistance.UnitOfWorks;
using StackExchange.Redis;

namespace NewsService.Persistance;

public static class ServiceRegistration
{
    public static IServiceCollection AddPersistanceServices(this IServiceCollection services, IConfiguration configuration)
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

        services.Configure<WeatherOptions>(configuration.GetSection(WeatherOptions.SectionName));

        var weatherTimeout = TimeSpan.FromSeconds(
            configuration.GetValue($"{WeatherOptions.SectionName}:TimeoutSeconds", 5));

        services.AddHttpClient<IWeatherClient, OpenMeteoWeatherClient>(client =>
            client.Timeout = weatherTimeout);

        services.AddHttpClient<IMarketClient, YahooMarketClient>(client =>
        {
            client.BaseAddress = new Uri("https://query1.finance.yahoo.com");

            // User-Agent ZORUNLU: başlıksız isteklere Yahoo 403 dönüyor.
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "Mozilla/5.0 (compatible; Telgraf/1.0)");

            client.Timeout = TimeSpan.FromSeconds(
                configuration.GetValue("Market:TimeoutSeconds", 5));
        });

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        // S3 client'ları pahalı ve thread-safe — istek başına yeniden kurulmamalı.
        services.AddSingleton<IStorageService, S3StorageService>();

        return services;
    }
}
