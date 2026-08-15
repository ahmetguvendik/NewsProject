using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;
using NewsService.Persistance.Caching;
using StackExchange.Redis;
using NewsService.Persistance.External;
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

        // Redis bağlantısı uygulama ömrü boyunca tektir (multiplexer thread-safe).
        // AbortOnConnectFail=false: Redis kapalıyken de servis ayağa kalkar ve
        // bağlantı geri geldiğinde kendiliğinden toparlar — önbellek erişilemez
        // olduğunda uygulamanın açılmaması kabul edilemez.
        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(configuration["Redis:Connection"] ?? "redis:6379");
            options.AbortOnConnectFail = false;
            // ConnectRetry varsayılanı 3: Redis kapalıyken her istek
            // 3 x ConnectTimeout kadar bekler ve site fiilen durur. Hızlı vazgeçip
            // veritabanına düşmek doğru davranış — RedisCacheService'teki devre
            // kesici de tekrar tekrar denenmesini engelliyor.
            options.ConnectRetry = 1;
            options.ConnectTimeout = 500;
            options.SyncTimeout = 500;
            options.AsyncTimeout = 500;
            return ConnectionMultiplexer.Connect(options);
        });
        services.AddSingleton<ICacheService, RedisCacheService>();

        // Dış servis: yanıt önbelleklendiği için nadiren çağrılıyor; yine de
        // kısa timeout ile bekletilmiyor.
        services.AddHttpClient<IWeatherClient, OpenMeteoWeatherClient>(client =>
            client.Timeout = TimeSpan.FromSeconds(5));

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        // S3 client'ları pahalı ve thread-safe — istek başına yeniden kurulmamalı.
        services.AddSingleton<IStorageService, S3StorageService>();

        return services;
    }
}
