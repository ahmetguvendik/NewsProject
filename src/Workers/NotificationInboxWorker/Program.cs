using Logging.Registration;
using Microsoft.EntityFrameworkCore;
using NotificationInboxWorker;
using NotificationService.Application.Interfaces;
using NotificationService.Persistance.Email;
using HealthCheck.Registration;

var builder = WebApplication.CreateBuilder(args);

builder.UseAppLogging("notification-inbox-worker");

builder.Services.AddAllElasticApm();

builder.Services.AddDbContext<InboxWorkerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddHttpClient<IIdentityContactClient, IdentityContactClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Identity:BaseUrl"] ?? "http://localhost:5001");
});

builder.Services.AddHostedService<Worker>();

var pollSeconds = builder.Configuration.GetValue("Inbox:PollIntervalSeconds", 5);

builder.Services.AddAppHealthChecks(builder.Configuration)
    .AddWorkerHeartbeat(TimeSpan.FromSeconds(Math.Max(30, pollSeconds * 4)))
    .AddPostgres()

    .AddDeadLetters((sp, ct) =>
        sp.GetRequiredService<InboxWorkerDbContext>().InboxMessages
            .CountAsync(m => m.IsDeadLettered, ct));

var host = builder.Build();

host.MapAppHealthChecks();

host.Run();
