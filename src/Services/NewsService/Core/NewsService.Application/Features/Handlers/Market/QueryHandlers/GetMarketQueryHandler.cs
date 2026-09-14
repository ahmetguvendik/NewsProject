using MediatR;
using NewsService.Application.Features.Queries.Market.Request;
using NewsService.Application.Features.Queries.Market.Response;
using NewsService.Application.Interfaces;

namespace NewsService.Application.Features.Handlers.Market.QueryHandlers;

public class GetMarketQueryHandler : IRequestHandler<GetMarketQuery, GetMarketResponse>
{
    private readonly IMarketClient _marketClient;

    public GetMarketQueryHandler(IMarketClient marketClient) => _marketClient = marketClient;

    public Task<GetMarketResponse> Handle(GetMarketQuery request, CancellationToken cancellationToken)
        => _marketClient.GetQuotesAsync(cancellationToken);
}
