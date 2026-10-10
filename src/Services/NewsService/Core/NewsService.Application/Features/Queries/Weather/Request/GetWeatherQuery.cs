using MediatR;
using NewsService.Application.Caching;
using NewsService.Application.Features.Queries.Weather.Response;

namespace NewsService.Application.Features.Queries.Weather.Request;

public class GetWeatherQuery : IRequest<GetWeatherResponse>, ICacheableQuery
{
    public string CacheKey => CacheKeys.Weather;

    // Hava durumu 30 dakikada bir tazelenir; daha sık çağırmanın karşılığı yok.
    public TimeSpan Duration => TimeSpan.FromMinutes(30);
}
