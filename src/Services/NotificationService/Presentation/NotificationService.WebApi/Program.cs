using HealthCheck.Registration;
using Logging.Registration;
using NotificationService.Infrastructure;
using NotificationService.Persistence;
using NotificationService.Persistence.Contexts;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.UseAppLogging("notification-service");

builder.UseAppApm();

builder.Services.AddPersistenceServices(builder.Configuration);
builder.Services.AddInfrastructureServices();

// Kafka burada gerçek bir bağımlılık: iki consumer bu process'in içinde koşuyor.
// Redis ve MinIO bu serviste kullanılmıyor.
builder.Services.AddAppHealthChecks(builder.Configuration)
    .AddPostgres()
    .AddKafka();

var app = builder.Build();

// Kafka consumer'ları InboxMessages'a yazdığı için şema Run()'dan önce hazır olmalı.
await app.MigrateDatabaseAsync<NotificationServiceDbContext>();

app.MapAppHealthChecks();

app.Run();
