using FluentValidation;
using IdentityService.Application.Auditing;
using IdentityService.Application.Behaviors;
using IdentityService.Application.Caching;
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

        // Keycloak parola olaylarını kendi log hattımıza taşıyan senkronizasyon.
        // Tetikleyicisi Presentation'da (KeycloakEventPollerService); iş burada.
        //
        // İmleç SINGLETON olmak zorunda: turlar arasında yaşamazsa her tur
        // "ilk tur" sayılır ve hiçbir olay loglanmaz.
        //
        // Sync ise scoped ve her turda yeni scope'tan alınıyor, çünkü taşıdığı
        // IKeycloakAdminClient bir typed HttpClient (AddHttpClient → transient).
        // Uzun ömürlü bir nesnede tutulsaydı IHttpClientFactory'nin handler
        // yenilemesi devre dışı kalırdı.
        //
        // TimeProvider: Sync imleci "şu an"a kuruyor; saat enjekte edilince bu
        // davranış sahte bir saatle test edilebiliyor. TryAdd, başka bir yerde
        // zaten kayıtlıysa üzerine yazmasın diye.
        services.AddSingleton<IKeycloakEventCursor, InMemoryKeycloakEventCursor>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<KeycloakEventSync>();

        return services;
    }
}
