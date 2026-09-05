namespace NewsService.Persistance.External;

public class WeatherOptions
{
    public const string SectionName = "Weather";

    /// <summary>
    /// Yanıtta gösterilen şehir adı. Koordinatlardan türetilmiyor; şehir değiştirilecekse
    /// Latitude/Longitude/Timezone ile birlikte güncellenmeli.
    /// </summary>
    public string City { get; set; } = "Ankara";

    public double Latitude { get; set; } = 39.9334;
    public double Longitude { get; set; } = 32.8597;

    /// <summary>Open-Meteo'nun beklediği IANA saat dilimi adı.</summary>
    public string Timezone { get; set; } = "Europe/Istanbul";

    /// <summary>
    /// Sorgu parametreleri Open-Meteo'ya özgü olduğu için bu adres sağlayıcı değiştirmek
    /// için değil, testte sahte uca yönlendirmek için var.
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.open-meteo.com/v1/forecast";

    /// <summary>Yanıt önbelleklendiği için çağrı nadir; yine de kullanıcı bekletilmiyor.</summary>
    public int TimeoutSeconds { get; set; } = 5;
}
