namespace NewsService.Persistance.External;

public class WeatherOptions
{
    public const string SectionName = "Weather";

    public string City { get; set; } = "Ankara";

    public double Latitude { get; set; } = 39.9334;
    public double Longitude { get; set; } = 32.8597;

    public string Timezone { get; set; } = "Europe/Istanbul";

    public string BaseUrl { get; set; } = "https://api.open-meteo.com/v1/forecast";

    public int TimeoutSeconds { get; set; } = 5;
}
