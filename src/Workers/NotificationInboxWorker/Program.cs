using Microsoft.EntityFrameworkCore;
using NotificationInboxWorker;
using NotificationService.Application.Interfaces;
using NotificationService.Persistance.Email;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<InboxWorkerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddHttpClient<IIdentityContactClient, IdentityContactClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Identity:BaseUrl"] ?? "http://localhost:5001");
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
