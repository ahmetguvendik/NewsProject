using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthCheck.Registration;

/// <summary>
/// Son N dakikadaki hata sayısı eşiği aşarsa Unhealthy döner.
///
/// Diğer kontrollerden farkı: onlar DURUM soruyor ("Postgres'e bağlanabiliyor
/// muyum"), bu DESEN soruyor ("son 5 dakikada kaç hata oldu"). Aradaki fark
/// pratikte şu: yeni bir deploy'da her yayınlama isteği 500 dönse, bağımlılık
/// kontrollerinin hepsi yeşil kalır — Postgres ayakta, Redis ayakta, hiçbir şey
/// "çökmüş" değil. Ama sistem bozuk. Bunu ancak logların kendisi gösterir.
///
/// Sağlık kontrolü olarak yazıldı, ayrı bir uyarı sistemine değil: mevcut panel
/// ve webhook zaten kurulu, uyarıların tek yerden çıkması iki ayrı sistemi doğru
/// yapılandırmaktan daha değerli.
/// </summary>
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

        // Elasticsearch'te kimlik doğrulama açık. Kullanıcı adı verilmemişse
        // başlık eklenmiyor: güvenliğin kapalı olduğu bir kurulumda da çalışsın.
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
                // Sorgu yapılamıyorsa "hata yok" demek yanlış olur: uyarının
                // kaynağı kör kalmış demektir ve bu da bilinmesi gereken bir durum.
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
