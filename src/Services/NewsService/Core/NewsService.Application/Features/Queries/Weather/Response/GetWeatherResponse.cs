namespace NewsService.Application.Features.Queries.Weather.Response;

public class GetWeatherResponse
{
    public string City { get; set; } = string.Empty;
    public int TemperatureC { get; set; }

    public string Icon { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
