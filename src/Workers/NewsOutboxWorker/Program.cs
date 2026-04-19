using Microsoft.EntityFrameworkCore;
using NewsOutboxWorker;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddDbContext<OutboxWorkerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
