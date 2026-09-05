using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NewsService.Application.Features.Queries.Weather.Response;
using NewsService.Application.Interfaces;
using Shared.Exceptions;

namespace NewsService.Persistance.External;

/// <summary>
/// Open-Meteo istemcisi — API anahtarı gerektirmiyor. Şehir ve koordinatlar
/// "Weather" ayar bölümünden gelir.
/// </summary>
public sealed class OpenMeteoWeatherClient : IWeatherClient
{
    private readonly HttpClient _httpClient;
    private readonly WeatherOptions _options;

    public OpenMeteoWeatherClient(HttpClient httpClient, IOptions<WeatherOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<GetWeatherResponse> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        // is_day olmadan gece ile gündüz ayırt edilemiyor; açık gökyüzü gece de
        // güneş ikonuyla gösteriliyordu. Alan bu yüzden açıkça isteniyor.
        var url = $"{_options.BaseUrl}" +
                  $"?latitude={_options.Latitude.ToString(CultureInfo.InvariantCulture)}" +
                  $"&longitude={_options.Longitude.ToString(CultureInfo.InvariantCulture)}" +
                  $"&current=temperature_2m,weather_code,is_day" +
                  $"&timezone={Uri.EscapeDataString(_options.Timezone)}";

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync(url, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ExternalServiceException("Open-Meteo", "Hava durumu servisine ulaşılamadı.", ex.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalServiceException(
                "Open-Meteo",
                "Hava durumu alınamadı.",
                $"Servis {(int)response.StatusCode} döndürdü.");
        }

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var current = document.RootElement.GetProperty("current");

        var code = current.GetProperty("weather_code").GetInt32();
        var isDay = current.GetProperty("is_day").GetInt32() == 1;

        return new GetWeatherResponse
        {
            City = _options.City,
            TemperatureC = (int)Math.Round(current.GetProperty("temperature_2m").GetDouble()),
            Icon = Icon(code, isDay),
            Description = Describe(code)
        };
    }

    /// <summary>
    /// WMO hava kodunu emojiye çevirir. Açık ve az bulutlu hallerde gündüz/gece
    /// ayrımı yapılır — kapalı, yağışlı ve sisli havalarda gökyüzü zaten görünmediği
    /// için ikon her iki durumda da aynı.
    /// </summary>
    private static string Icon(int code, bool isDay) => code switch
    {
        0 => isDay ? "☀️" : "🌙",
        1 or 2 => isDay ? "🌤️" : "☁️",
        3 => "☁️",
        45 or 48 => "🌫️",
        >= 51 and <= 57 => "🌦️",
        >= 61 and <= 67 => "🌧️",
        >= 71 and <= 77 => "❄️",
        >= 80 and <= 82 => "🌦️",
        >= 85 and <= 86 => "🌨️",
        >= 95 => "⛈️",
        _ => "🌡️"
    };

    private static string Describe(int code) => code switch
    {
        0 => "Açık",
        1 => "Az bulutlu",
        2 => "Parçalı bulutlu",
        3 => "Kapalı",
        45 or 48 => "Sisli",
        >= 51 and <= 57 => "Çiseleyen yağmur",
        >= 61 and <= 67 => "Yağmurlu",
        >= 71 and <= 77 => "Karlı",
        >= 80 and <= 82 => "Sağanak yağışlı",
        >= 85 and <= 86 => "Kar sağanağı",
        >= 95 => "Gök gürültülü fırtına",
        _ => "Bilinmiyor"
    };
}
