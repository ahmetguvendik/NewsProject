using MediatR;
using NewsService.Application.Caching;
using NewsService.Application.Features.Queries.Market.Response;

namespace NewsService.Application.Features.Queries.Market.Request;

/// <summary>
/// Piyasa şeridi her sayfa yüklemesinde görünüyor ve dört ayrı sembol çekiyor —
/// yani önbelleksiz her ziyaretçi Yahoo'ya dört istek attırırdı. Hava durumunda
/// olduğu gibi çağrı sunucudan yapılıyor ve önbellekleniyor; ziyaretçi sayısından
/// bağımsız olarak dakikada bir tur atılıyor.
///
/// Süre hava durumundan kısa (30 dk yerine 1 dk): borsa gün içinde sürekli
/// hareket ediyor, yarım saatlik veri yanıltıcı olurdu.
/// </summary>
public class GetMarketQuery : IRequest<GetMarketResponse>, ICacheableQuery
{
    public string CacheKey => CacheKeys.Market;

    public TimeSpan Duration => TimeSpan.FromMinutes(1);
}
