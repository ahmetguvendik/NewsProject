using NewsService.Application.Features.Queries.Market.Response;

namespace NewsService.Application.Interfaces;

public interface IMarketClient
{
    Task<GetMarketResponse> GetQuotesAsync(CancellationToken cancellationToken = default);
}
