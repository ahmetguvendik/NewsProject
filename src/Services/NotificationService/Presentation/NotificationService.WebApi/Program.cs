using HealthCheck.Registration;
using Logging.Registration;
using NotificationService.Persistance;
using NotificationService.Persistance.Contexts;
using Shared.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.UseAppLogging("notification-service");

builder.Services.AddAllElasticApm();

builder.Services.AddPersistanceServices(builder.Configuration);

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
