using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NewsService.Application.Auditing.Abstract;
using NewsService.Application.Auditing.Concrete;
using NewsService.Application.Behaviors;
using NewsService.Application.Features.Queries.Article;

namespace NewsService.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IChangeAuditCollector, ChangeAuditCollector>();
        services.Configure<FeedOptions>(configuration.GetSection(FeedOptions.SectionName));

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(ServiceRegistration).Assembly));

        services.AddValidatorsFromAssembly(typeof(ServiceRegistration).Assembly);
        // Loglama en dışta: doğrulama hatasıyla reddedilen komut hiç çalışmadığı
        // için "tamamlandı" satırı da yazılmıyor, süre ölçümü de gerçek işi kapsıyor.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CommandLoggingBehavior<,>));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Sıra önemli: doğrulama önce çalışsın ki geçersiz istekler önbelleğe ulaşmasın.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));

        return services;
    }
}
