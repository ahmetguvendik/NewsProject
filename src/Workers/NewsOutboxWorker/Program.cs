using Logging.Registration;
using Microsoft.EntityFrameworkCore;
using NewsOutboxWorker;
using HealthCheck.Registration;

var builder = WebApplication.CreateBuilder(args);

builder.UseAppLogging("news-outbox-worker");

builder.Services.AddAllElasticApm();

builder.Services.AddDbContext<OutboxWorkerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<Worker>();

var pollSeconds = builder.Configuration.GetValue("Outbox:PollIntervalSeconds", 15);

builder.Services.AddAppHealthChecks(builder.Configuration)
    .AddWorkerHeartbeat(TimeSpan.FromSeconds(Math.Max(30, pollSeconds * 4)))
    .AddPostgres()

    .AddDeadLetters((sp, ct) =>
        sp.GetRequiredService<OutboxWorkerDbContext>().OutboxMessages
            .CountAsync(m => m.IsDeadLettered, ct));

var host = builder.Build();

host.MapAppHealthChecks();

host.Run();
