namespace NewsService.Application.Features.Queries.Market.Response;

public class MarketQuote
{
    public string Label { get; set; } = string.Empty;

    public decimal Value { get; set; }

    public decimal ChangePercent { get; set; }
}

public class GetMarketResponse
{
    public List<MarketQuote> Quotes { get; set; } = [];

    public DateTime FetchedAt { get; set; }
}
