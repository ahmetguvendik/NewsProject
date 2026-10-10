using NewsService.Application.Features.Queries.Weather.Response;

namespace NewsService.Application.Interfaces;

public interface IWeatherClient
{
    Task<GetWeatherResponse> GetCurrentAsync(CancellationToken cancellationToken = default);
}
