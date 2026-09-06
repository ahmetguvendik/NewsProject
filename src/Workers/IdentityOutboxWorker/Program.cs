using IdentityOutboxWorker;
using Microsoft.EntityFrameworkCore;
using Shared.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

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
    .AddPostgres();

var host = builder.Build();

host.MapAppHealthChecks();

host.Run();
