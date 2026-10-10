using MediatR;
using NewsService.Application.Caching;
using NewsService.Application.Features.Queries.Market.Response;

namespace NewsService.Application.Features.Queries.Market.Request;

public class GetMarketQuery : IRequest<GetMarketResponse>, ICacheableQuery
{
    public string CacheKey => CacheKeys.Market;

    public TimeSpan Duration => TimeSpan.FromMinutes(1);
}
