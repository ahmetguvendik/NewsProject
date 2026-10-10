using FluentValidation;
using IdentityService.Application.Auditing;
using IdentityService.Application.Behaviors;
using IdentityService.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace IdentityService.Application;

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

        services.AddSingleton<IKeycloakEventCursor, InMemoryKeycloakEventCursor>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<KeycloakEventSync>();

        return services;
    }
}
