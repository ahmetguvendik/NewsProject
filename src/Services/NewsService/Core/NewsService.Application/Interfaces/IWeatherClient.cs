using NewsService.Application.Features.Queries.Weather.Response;

namespace NewsService.Application.Interfaces;

/// <summary>Dış hava durumu servisi (Open-Meteo).</summary>
public interface IWeatherClient
{
    Task<GetWeatherResponse> GetCurrentAsync(CancellationToken cancellationToken = default);
}
