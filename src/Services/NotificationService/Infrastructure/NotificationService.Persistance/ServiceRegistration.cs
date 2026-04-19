using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NotificationService.Application.Interfaces;
using NotificationService.Persistance.Consumers;
using NotificationService.Persistance.Contexts;
using NotificationService.Persistance.Email;
using NotificationService.Persistance.Repositories;

namespace NotificationService.Persistance;

public static class ServiceRegistration
{
    public static IServiceCollection AddPersistanceServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<NotificationServiceDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IEmailService, EmailService>();

        // Kafka consumers — sadece InboxMessages'a kaydeder
        services.AddHostedService<UserRegisteredConsumer>();
        services.AddHostedService<ArticlePublishedConsumer>();

        return services;
    }
}
