using Elastic.CommonSchema.Serilog;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Sinks.PeriodicBatching;

namespace Logging.Registration;

public static class LoggingExtensions
{
    /// <summary>
    /// Serilog'u iki hedefle kurar: Console (her zaman) ve Kafka (yapılandırılmışsa).
    ///
    /// Console asla kapatılmıyor. Kafka, Logstash ya da Elasticsearch'ten herhangi
    /// biri çökse bile "docker compose logs" çalışmaya devam etmeli — bir kesintiyi
    /// teşhis ederken ilk bakılan yer orası ve tam o anda kör kalmak istemeyiz.
    ///
    /// İkisi de ECS formatında yazıyor. Böylece stdout ile Elasticsearch'teki kayıt
    /// aynı şemayı paylaşıyor ve ileride Elastic APM eklendiğinde alan adları
    /// (trace.id, service.name, log.level) hazır uyuyor.
    /// </summary>
    public static IHostBuilder UseAppLogging(this IHostBuilder host, string serviceName) =>
        host.UseSerilog((context, services, configuration) =>
            Configure(configuration, context.Configuration, context.HostingEnvironment, serviceName, services));

    /// <summary>
    /// Minimal host (WebApplicationBuilder) için aynı kurulum.
    /// </summary>
    public static void UseAppLogging(this IHostApplicationBuilder builder, string serviceName)
    {
        // Enricher'ın ihtiyaç duyduğu HttpContextAccessor kaydediliyor.
        builder.Services.AddHttpContextAccessor();

        var configuration = new LoggerConfiguration();
        Configure(configuration, builder.Configuration, builder.Environment, serviceName, null);

        Log.Logger = configuration.CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger);
    }

    private static void Configure(
        LoggerConfiguration logger,
        IConfiguration configuration,
        IHostEnvironment environment,
        string serviceName,
        IServiceProvider? services)
    {
        logger
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.With<TraceEnricher>()

            // Eylemi yapan kullanıcı. HttpContext gerektirdiği için servis
            // sağlayıcıdan çözülüyor; worker'larda istek olmadığından sessiz kalır.
            .Enrich.With(new UserEnricher(
                services?.GetService<IHttpContextAccessor>() ?? new HttpContextAccessor()))

            // ECS'in beklediği adlar. Elastic APM aynı alanlarla çalıştığı için
            // servis adı burada ne ise APM tarafında da o olmalı.
            .Enrich.WithProperty("service.name", serviceName)
            .Enrich.WithProperty("service.environment", environment.EnvironmentName)

            .WriteTo.Console(new EcsTextFormatter());

        var bootstrapServers = configuration["Logging:Kafka:BootstrapServers"]
                               ?? configuration["Kafka:BootstrapServers"];

        // Kafka ayarı yoksa sink hiç kurulmuyor: yerel geliştirmede Kafka'sız
        // çalışabilmek için. Console zaten devrede olduğundan log kaybı olmuyor.
        if (string.IsNullOrWhiteSpace(bootstrapServers))
            return;

        var topic = configuration["Logging:Kafka:Topic"] ?? "logs";

        var batching = new PeriodicBatchingSinkOptions
        {
            BatchSizeLimit = configuration.GetValue("Logging:Kafka:BatchSize", 500),
            Period = TimeSpan.FromSeconds(configuration.GetValue("Logging:Kafka:FlushSeconds", 2)),

            // Kuyruk dolduğunda yeni olaylar DÜŞÜRÜLÜR, çağıran beklemez.
            // Logstash ya da Kafka yavaşladığında uygulamanın yavaşlamaması için
            // bilinçli bir takas: log kaybı, istek kuyruğunun tıkanmasından iyidir.
            QueueLimit = configuration.GetValue("Logging:Kafka:QueueLimit", 10_000),
            EagerlyEmitFirstEvent = false
        };

        var kafkaSink = new PeriodicBatchingSink(
            new KafkaLogSink(bootstrapServers, topic),
            batching);

        logger.WriteTo.Async(sink => sink.Sink(kafkaSink));
    }
}
