using Microsoft.Extensions.DependencyInjection;
using NotificationService.Infrastructure.Consumers;

namespace NotificationService.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Kafka consumers — sadece InboxMessages'a kaydeder
        services.AddHostedService<UserRegisteredConsumer>();
        services.AddHostedService<ArticlePublishedConsumer>();

        return services;
    }
}
