namespace NewsService.Application.Features.Queries.Market.Response;

/// <summary>Şeritte gösterilen tek bir piyasa değeri.</summary>
public class MarketQuote
{
    /// <summary>Ekranda görünen ad: "BIST 100", "Dolar"…</summary>
    public string Label { get; set; } = string.Empty;

    public decimal Value { get; set; }

    /// <summary>Önceki kapanışa göre yüzde değişim. Yön ve renk buradan belirleniyor.</summary>
    public decimal ChangePercent { get; set; }
}

public class GetMarketResponse
{
    public List<MarketQuote> Quotes { get; set; } = [];

    /// <summary>
    /// Verinin alındığı an. Şeritte gösterilmiyor ama önbellekten mi taze mi
    /// geldiğini anlamak için tanı değeri var.
    /// </summary>
    public DateTime FetchedAt { get; set; }
}
