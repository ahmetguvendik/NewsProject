using Logging.Registration;
using Microsoft.EntityFrameworkCore;
using NewsOutboxWorker;
using HealthCheck.Registration;

var builder = WebApplication.CreateBuilder(args);

builder.UseAppLogging("news-outbox-worker");

// Elastic APM. Worker'da HTTP trafiği yok denecek kadar az — buradaki değer
// EF Core sorguları ve, asıl önemlisi, servislerden gelen trace'in devamını
// görebilmek: outbox satırındaki traceparent geri kurulduğu için worker'ın
// yaptığı iş isteği başlatan trace'in altında görünüyor.
builder.Services.AddAllElasticApm();

builder.Services.AddDbContext<OutboxWorkerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<Worker>();

// Worker'ın kendi sağlık ucu. Minimal web host'a geçilmesinin tek sebebi bu: aksi
// halde panel altı process'in yalnızca üçünü görüyordu ve worker'lar — sessizce
// bozulmaya en açık parçalar — tamamen izlemesizdi.
//
// Eşik tarama aralığının 4 katı: tek bir yavaş tur yanlış alarm üretmesin.
var pollSeconds = builder.Configuration.GetValue("Outbox:PollIntervalSeconds", 5);

builder.Services.AddAppHealthChecks(builder.Configuration)
    .AddWorkerHeartbeat(TimeSpan.FromSeconds(Math.Max(30, pollSeconds * 4)))
    .AddPostgres()

    // Ölü mesajlar sessizce birikmesin: sıfırdan büyükse panel kırmızıya döner
    // ve webhook tetiklenir. Container sağlıklı kalır (Dependency etiketi) —
    // worker bozuk değil, işlenemeyen bir mesaj var.
    .AddDeadLetters((sp, ct) =>
        sp.GetRequiredService<OutboxWorkerDbContext>().OutboxMessages
            .CountAsync(m => m.IsDeadLettered, ct));

var host = builder.Build();

host.MapAppHealthChecks();

host.Run();
