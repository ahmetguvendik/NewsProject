using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NewsService.Application.Behaviors;
using NewsService.Application.Caching;

namespace NewsService.Application;

public static class ServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
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
