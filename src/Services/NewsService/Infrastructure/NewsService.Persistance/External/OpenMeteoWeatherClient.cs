using System.Text.Json;
using NewsService.Application.Features.Queries.Weather.Response;
using NewsService.Application.Interfaces;
using Shared.Exceptions;

namespace NewsService.Persistance.External;

/// <summary>
/// Open-Meteo istemcisi — API anahtarı gerektirmiyor. Şehir sabit: Ankara.
/// </summary>
public sealed class OpenMeteoWeatherClient : IWeatherClient
{
    private const string City = "Ankara";
    private const double Latitude = 39.9334;
    private const double Longitude = 32.8597;

    private readonly HttpClient _httpClient;

    public OpenMeteoWeatherClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<GetWeatherResponse> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        // is_day olmadan gece ile gündüz ayırt edilemiyor; açık gökyüzü gece de
        // güneş ikonuyla gösteriliyordu. Alan bu yüzden açıkça isteniyor.
        var url = $"https://api.open-meteo.com/v1/forecast" +
                  $"?latitude={Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                  $"&longitude={Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}" +
                  $"&current=temperature_2m,weather_code,is_day" +
                  $"&timezone=Europe%2FIstanbul";

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
            City = City,
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
