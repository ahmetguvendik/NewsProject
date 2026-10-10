using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheck.Registration;

public static class HealthCheckExtensions
{
    public static AppHealthChecksBuilder AddAppHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration) =>
        new(services.AddHealthChecks(), configuration);

    public static IEndpointRouteBuilder MapAppHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // live hiçbir kontrol çalıştırmaz: bağımlılık eklenirse Postgres takılınca orkestratör tüm servisleri yeniden başlatır.
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        }).AllowAnonymous();

        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthCheckTags.Ready),
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        }).AllowAnonymous();

        endpoints.MapHealthChecks("/health", new HealthCheckOptions
        {
            // Panel yalnızca UI şemasını okuyabiliyor.
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,

            // Degraded 200: tolere edilen kesinti servisin çalışmadığı anlamına gelmez.
            ResultStatusCodes =
            {
                [HealthStatus.Healthy] = StatusCodes.Status200OK,
                [HealthStatus.Degraded] = StatusCodes.Status200OK,
                [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
            }
        }).AllowAnonymous();

        return endpoints;
    }
}
