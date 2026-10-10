using HealthCheck.Api;
using HealthCheck.Registration;
using HealthChecks.UI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

builder.Services.AddHealthChecks().Add(new HealthCheckRegistration(
    name: "hata-orani",
    factory: sp => new ErrorRateHealthCheck(
        sp.GetRequiredService<IHttpClientFactory>(),
        elasticsearchUrl: builder.Configuration["Elasticsearch:Url"] ?? "http://elasticsearch:9200",
        indexPattern: builder.Configuration["Alerting:ErrorRate:IndexPattern"] ?? "logs-*",
        windowMinutes: builder.Configuration.GetValue("Alerting:ErrorRate:WindowMinutes", 5),
        threshold: builder.Configuration.GetValue("Alerting:ErrorRate:Threshold", 10),
        username: builder.Configuration["Elasticsearch:Username"],
        password: builder.Configuration["Elasticsearch:Password"]),
    failureStatus: HealthStatus.Unhealthy,
    tags: [HealthCheckTags.Dependency],
    timeout: TimeSpan.FromSeconds(5)));

builder.Services
    .AddHealthChecksUI(settings =>
    {
        settings.SetEvaluationTimeInSeconds(
            builder.Configuration.GetValue("HealthChecksUI:EvaluationSeconds", 100));

        settings.SetApiMaxActiveRequests(1);
    })
    // In-memory: panel yeniden başladığında geçmiş sıfırlanır. Anlık durum için
    // yeterli; kalıcı geçmiş isteniyorsa Postgres sağlayıcısına geçilir.
    .AddInMemoryStorage();

var app = builder.Build();

// Panel kök adreste.
app.MapHealthChecksUI(options => options.UIPath = "/");

// Kendi sağlık uçları: /health, /health/live, /health/ready.
app.MapAppHealthChecks();

app.MapGet("/api/health", async (HealthChecksDb db, CancellationToken cancellationToken) =>
{
    var configurations = await db.Configurations.ToListAsync(cancellationToken);

    var latest = await db.Executions
        .Include(e => e.Entries)
        .ToListAsync(cancellationToken);

    var items = configurations.Select(config =>
    {
        var execution = latest.FirstOrDefault(e => e.Name == config.Name);

        return new
        {
            name = config.Name,
            uri = config.Uri,
            status = execution?.Status.ToString() ?? "Unknown",
            lastExecuted = execution?.LastExecuted,
            checks = execution?.Entries.Select(entry => new
            {
                name = entry.Name,
                status = entry.Status.ToString(),
                description = entry.Description,
                duration = entry.Duration
            })
        };
    }).ToList();

    var anyUnhealthy = items.Any(i => i.status is "Unhealthy" or "Unknown");

    return Results.Json(new
    {
        status = anyUnhealthy ? "Unhealthy" : "Healthy",
        total = items.Count,
        services = items
    }, statusCode: anyUnhealthy ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status200OK);
});

app.Run();
