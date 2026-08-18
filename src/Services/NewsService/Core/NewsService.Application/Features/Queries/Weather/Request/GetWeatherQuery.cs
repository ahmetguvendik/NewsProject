using MediatR;
using NewsService.Application.Caching;
using NewsService.Application.Features.Queries.Weather.Response;

namespace NewsService.Application.Features.Queries.Weather.Request;

/// <summary>
/// Hava durumu her sayfa yüklemesinde gösteriliyor. Daha önce her ziyaretçinin
/// tarayıcısı doğrudan Open-Meteo'ya gidiyordu; trafik arttığında dış servisten
/// rate limit yenip widget'ın herkeste birden kırılması riski vardı.
///
/// Artık çağrı sunucudan yapılıyor ve önbelleklendiği için ziyaretçi sayısından
/// bağımsız olarak 30 dakikada bir tek istek gidiyor.
/// </summary>
public class GetWeatherQuery : IRequest<GetWeatherResponse>, ICacheableQuery
{
    public string CacheKey => CacheKeys.Weather;

    // Hava durumu 30 dakikada bir tazelenir; daha sık çağırmanın karşılığı yok.
    public TimeSpan Duration => TimeSpan.FromMinutes(30);
}
