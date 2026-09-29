using FluentValidation;
using IdentityService.Application.Auditing;
using IdentityService.Application.Behaviors;
using IdentityService.Application.Caching;
using IdentityService.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

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

        // Keycloak parola olaylarını kendi log hattımıza taşıyan senkronizasyon.
        // Tetikleyicisi Presentation'da (KeycloakEventPollerService); iş burada.
        //
        // İmleç SINGLETON olmak zorunda: turlar arasında yaşamazsa her tur
        // "ilk tur" sayılır ve hiçbir olay loglanmaz. Sync ise scoped —
        // IKeycloakAdminClient scoped olduğu için onunla aynı ömürde.
        services.AddSingleton<IKeycloakEventCursor, InMemoryKeycloakEventCursor>();
        services.AddScoped<KeycloakEventSync>();

        return services;
    }
}
