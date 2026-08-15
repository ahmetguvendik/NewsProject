namespace NewsService.Application.Features.Queries.Weather.Response;

public class GetWeatherResponse
{
    public string City { get; set; } = string.Empty;
    public int TemperatureC { get; set; }

    /// <summary>Gündüz/gece ayrımı uygulanmış hazır ikon — client'ın eşleme mantığı taşımasına gerek kalmıyor.</summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>Kısa Türkçe açıklama (ör. "Parçalı bulutlu").</summary>
    public string Description { get; set; } = string.Empty;
}
