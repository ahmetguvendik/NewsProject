using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheck.Api;

internal sealed class ErrorRateHealthCheck : IHealthCheck
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _elasticsearchUrl;
    private readonly string _indexPattern;
    private readonly int _windowMinutes;
    private readonly int _threshold;
    private readonly AuthenticationHeaderValue? _authorization;

    public ErrorRateHealthCheck(
        IHttpClientFactory httpClientFactory,
        string elasticsearchUrl,
        string indexPattern,
        int windowMinutes,
        int threshold,
        string? username,
        string? password)
    {
        _httpClientFactory = httpClientFactory;
        _elasticsearchUrl = elasticsearchUrl.TrimEnd('/');
        _indexPattern = indexPattern;
        _windowMinutes = windowMinutes;
        _threshold = threshold;
        if (!string.IsNullOrEmpty(username))
        {
            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{username}:{password}"));

            _authorization = new AuthenticationHeaderValue("Basic", credentials);
        }
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("elasticsearch");

            var query = new
            {
                query = new
                {
                    @bool = new
                    {
                        must = new object[]
                        {
                            new { term = new Dictionary<string, string> { ["log.level"] = "Error" } },
                            new { range = new Dictionary<string, object>
                            {
                                ["@timestamp"] = new { gte = $"now-{_windowMinutes}m" }
                            }}
                        }
                    }
                }
            };

            using var request = new HttpRequestMessage(
                HttpMethod.Post, $"{_elasticsearchUrl}/{_indexPattern}/_count")
            {
                Content = JsonContent.Create(query)
            };

            request.Headers.Authorization = _authorization;

            using var response = await client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Sorgu yapılamıyorsa "hata yok" denmez: uyarının kör kaldığı da bilinmeli.
                return HealthCheckResult.Unhealthy(
                    $"Hata sayısı okunamadı: Elasticsearch {(int)response.StatusCode} döndü.");
            }

            using var document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));

            var count = document.RootElement.GetProperty("count").GetInt32();

            return count > _threshold
                ? HealthCheckResult.Unhealthy(
                    $"Son {_windowMinutes} dakikada {count} hata var (eşik {_threshold}).")
                : HealthCheckResult.Healthy(
                    $"Son {_windowMinutes} dakikada {count} hata (eşik {_threshold}).");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Hata oranı sorgulanamadı.", ex);
        }
    }
}
