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

    /// <summary>
    /// Üç uç açar:
    ///
    /// <c>/health/live</c>  — hiçbir kontrol çalıştırmaz, yalnızca process'in cevap
    /// verdiğini gösterir. Bağımlılık eklemek tehlikeli olurdu: Postgres bir an
    /// takıldığında orkestratör bütün servisleri yeniden başlatır ve kesintiyi büyütür.
    ///
    /// <c>/health/ready</c> — yalnızca <see cref="HealthCheckTags.Ready"/> etiketli
    /// kontroller. Trafik yönlendirme kararı buna bakar.
    ///
    /// <c>/health</c>       — hepsi, tek tek durumlarıyla. İnsan/panel içindir,
    /// otomatik karar için değil.
    /// </summary>
    public static IEndpointRouteBuilder MapAppHealthChecks(this IEndpointRouteBuilder endpoints)
    {
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
            // Özel bir format yerine UI'ın beklediği şema kullanılıyor: HealthDashboard
            // bu ucu okuyor ve kendi şemasından başkasını çözemiyor.
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,

            // Degraded'ı 200 ile dönüyoruz: tolere edilen bir bağımlılık kesintisi
            // servisin çalışmadığı anlamına gelmiyor ve 503 yanlış alarm üretirdi.
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
