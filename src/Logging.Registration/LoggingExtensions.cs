using Elastic.CommonSchema.Serilog;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Configuration;

namespace Logging.Registration;

public static class LoggingExtensions
{
    // Console her zaman açık: Kafka/Logstash/Elasticsearch çökse de "docker compose logs" çalışmalı.
    public static void UseAppLogging(this IHostApplicationBuilder builder, string serviceName)
    {
        // Zorunlu: ASP.NET Core HttpContext'i ancak bu kayıt varsa accessor'a yazar; yoksa UserEnricher hep boş görür.
        builder.Services.AddHttpContextAccessor();

        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.With<TraceEnricher>()
            .Enrich.With(new UserEnricher(new HttpContextAccessor()))
            .Enrich.WithProperty("service.name", serviceName)
            .Enrich.WithProperty("service.environment", builder.Environment.EnvironmentName)
            .WriteTo.Console(new EcsTextFormatter());

        AddKafkaSink(logger, builder.Configuration);

        Log.Logger = logger.CreateLogger();

        builder.Logging.ClearProviders();
        // dispose: kapanışta kuyrukta bekleyen loglar da Kafka'ya gönderilsin.
        builder.Logging.AddSerilog(Log.Logger, dispose: true);
    }

    // Kafka ayarı yoksa sink kurulmaz; yerelde Kafka'sız çalışılabilsin.
    private static void AddKafkaSink(LoggerConfiguration logger, IConfiguration configuration)
    {
        var bootstrapServers = configuration["Logging:Kafka:BootstrapServers"]
                               ?? configuration["Kafka:BootstrapServers"];

        if (string.IsNullOrWhiteSpace(bootstrapServers))
            return;

        var topic = configuration["Logging:Kafka:Topic"] ?? "logs";

        logger.WriteTo.Sink(new KafkaLogSink(bootstrapServers, topic), new BatchingOptions
        {
            BatchSizeLimit = configuration.GetValue("Logging:Kafka:BatchSize", 500),
            BufferingTimeLimit = TimeSpan.FromSeconds(configuration.GetValue("Logging:Kafka:FlushSeconds", 2)),
            // Kuyruk dolunca yeni olaylar düşürülür: log kaybı, isteklerin yavaşlamasından iyidir.
            QueueLimit = configuration.GetValue("Logging:Kafka:QueueLimit", 10_000),
            EagerlyEmitFirstEvent = false
        });
    }
}
