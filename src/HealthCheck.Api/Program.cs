using HealthChecks.UI.Data;
using Microsoft.EntityFrameworkCore;

// Altı process'in (üç servis, üç worker) sağlığını toplayan servis.
//
// Neden ayrı bir container: izlediği servislerden birinin içinde koşsaydı, o servis
// çöktüğünde panel de erişilemez olurdu — tam ihtiyaç duyulduğu anda.
//
// Kontrolleri KENDİSİ yapmıyor, servislerin /health uçlarını yokluyor. Kontrol,
// ölçtüğü process'in içinde olmak zorunda: buradan Postgres'e bağlanabilmek
// "news-service Postgres'e ulaşabiliyor" demek değil — o servisin bağlantı havuzu
// tükenmiş ya da parolası yanlış olabilir. Ayrıca compose'un service_healthy'si
// container başına sinyal istiyor, dışarıdan üretilemez.

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddHealthChecksUI(settings =>
    {
        settings.SetEvaluationTimeInSeconds(
            builder.Configuration.GetValue("HealthChecksUI:EvaluationSeconds", 15));

        settings.SetApiMaxActiveRequests(1);

        // İzlenen uçlar ayardan geliyor (HealthChecksUI:HealthChecks). Kodda gömülü
        // olsaydı yeni bir servis eklendiğinde bu projeyi yeniden derlemek gerekirdi.
    })
    // In-memory: panel yeniden başladığında geçmiş sıfırlanır. Anlık durum için
    // yeterli; kalıcı geçmiş isteniyorsa Postgres sağlayıcısına geçilir.
    .AddInMemoryStorage();

var app = builder.Build();

// Panel kök adreste.
app.MapHealthChecksUI(options => options.UIPath = "/");

// Toplu durum, makine tarafından okunabilir biçimde. Panel bir insan arayüzü;
// bu uç script'ler, izleme araçları ve Grafana gibi tüketiciler için.
//
// Kritik nokta HTTP kodu: herhangi bir process Unhealthy ise 503 dönüyor, böylece
// tüketicinin gövdeyi ayrıştırmasına gerek kalmadan "sistemde sorun var" sinyali
// alınabiliyor.
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
