using System.Text.Json;
using Microsoft.Extensions.Logging;
using NewsService.Application.Features.Queries.Market.Response;
using NewsService.Application.Interfaces;

namespace NewsService.Persistance.External;

/// <summary>
/// Piyasa değerlerini Yahoo Finance'ten okur.
///
/// RESMÎ BİR API DEĞİL. query1.finance.yahoo.com belgelenmemiş bir uç; anahtar
/// istemiyor ama Yahoo haber vermeden değiştirebilir ya da kapatabilir. Bu
/// yüzden hiçbir hata yukarı taşınmıyor: veri alınamazsa boş liste dönüyor ve
/// şerit arayüzde sessizce gizleniyor. Piyasa şeridi süs; yokluğu sayfayı
/// bozmamalı.
///
/// User-Agent ZORUNLU: başlıksız isteklere Yahoo 403 dönüyor.
/// </summary>
public class YahooMarketClient : IMarketClient
{
    /// <summary>Bir ons altının gram karşılığı — gram altın bu oranla hesaplanıyor.</summary>
    private const decimal GramsPerOunce = 31.1035m;

    /// <summary>
    /// Çekilecek semboller ve ekranda görünecek adları.
    ///
    /// Gram altın burada YOK çünkü Yahoo onu yayınlamıyor: ons altın (GC=F)
    /// dolar cinsinden geliyor ve dolar kuruyla çarpılıp grama bölünüyor.
    /// </summary>
    private static readonly (string Symbol, string Label)[] Symbols =
    [
        ("XU100.IS", "BIST 100"),
        ("USDTRY=X", "Dolar"),
        ("EURTRY=X", "Euro")
    ];

    private const string GoldSymbol = "GC=F";
    private const string UsdSymbol = "USDTRY=X";

    private readonly HttpClient _httpClient;
    private readonly ILogger<YahooMarketClient> _logger;

    public YahooMarketClient(HttpClient httpClient, ILogger<YahooMarketClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<GetMarketResponse> GetQuotesAsync(CancellationToken cancellationToken = default)
    {
        var response = new GetMarketResponse { FetchedAt = DateTime.UtcNow };

        try
        {
            // Dört sembol PARALEL çekiliyor. Sırayla gitseydi her biri ~200 ms'den
            // toplam gecikme kullanıcının gördüğü ilk isteğe biniyordu; sonuç zaten
            // bir dakika önbelleklendiği için maliyet tek seferlik ama yine de
            // ilk ziyaretçiyi bekletmenin anlamı yok.
            var wanted = Symbols.Select(s => s.Symbol).Append(GoldSymbol).ToArray();

            var results = await Task.WhenAll(
                wanted.Select(symbol => FetchAsync(symbol, cancellationToken)));

            var bySymbol = wanted
                .Zip(results)
                .Where(pair => pair.Second is not null)
                .ToDictionary(pair => pair.First, pair => pair.Second!.Value);

            foreach (var (symbol, label) in Symbols)
            {
                if (bySymbol.TryGetValue(symbol, out var quote))
                    response.Quotes.Add(Build(label, quote.Price, quote.PreviousClose));
            }

            // Gram altın: ons fiyatı dolar cinsinden, dolar kuruyla çarpılıp
            // grama bölünüyor. İkisinden biri alınamadıysa hiç gösterilmiyor —
            // yarım veriyle yanlış bir rakam üretmektense eksik göstermek yeğ.
            if (bySymbol.TryGetValue(GoldSymbol, out var gold) &&
                bySymbol.TryGetValue(UsdSymbol, out var usd))
            {
                response.Quotes.Add(Build(
                    "Gram altın",
                    gold.Price * usd.Price / GramsPerOunce,
                    gold.PreviousClose * usd.PreviousClose / GramsPerOunce));
            }
        }
        catch (Exception ex)
        {
            // Bilinçli olarak yutuluyor: şerit süs, yokluğu sayfayı bozmamalı.
            _logger.LogWarning(ex, "Piyasa verileri alınamadı; şerit gizlenecek.");
        }

        return response;
    }

    private static MarketQuote Build(string label, decimal value, decimal previousClose)
    {
        // Önceki kapanış sıfırsa (Yahoo bazen veriyi eksik döndürüyor) yüzde
        // hesabı sıfıra bölme olurdu; o durumda değişim yok sayılıyor.
        var changePercent = previousClose == 0
            ? 0
            : (value - previousClose) / previousClose * 100;

        return new MarketQuote
        {
            Label = label,
            Value = Math.Round(value, 2),
            ChangePercent = Math.Round(changePercent, 2)
        };
    }

    private async Task<(decimal Price, decimal PreviousClose)?> FetchAsync(
        string symbol, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"/v8/finance/chart/{Uri.EscapeDataString(symbol)}?interval=1d&range=1d";

            using var httpResponse = await _httpClient.GetAsync(url, cancellationToken);

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Yahoo {Symbol} için {StatusCode} döndü.",
                    symbol, (int)httpResponse.StatusCode);
                return null;
            }

            using var document = JsonDocument.Parse(
                await httpResponse.Content.ReadAsStringAsync(cancellationToken));

            var meta = document.RootElement
                .GetProperty("chart").GetProperty("result")[0].GetProperty("meta");

            var price = meta.GetProperty("regularMarketPrice").GetDecimal();

            // Alan adı sembole göre değişiyor: endekslerde chartPreviousClose,
            // döviz ve emtiada previousClose geliyor.
            var previousClose =
                meta.TryGetProperty("chartPreviousClose", out var chartPrevious)
                    ? chartPrevious.GetDecimal()
                    : meta.TryGetProperty("previousClose", out var previous)
                        ? previous.GetDecimal()
                        : price;

            return (price, previousClose);
        }
        catch (Exception ex)
        {
            // Tek sembolün düşmesi diğerlerini götürmesin: null dönüp listeden
            // çıkıyor, şerit kalan değerlerle çiziliyor.
            _logger.LogWarning(ex, "Yahoo {Symbol} okunamadı.", symbol);
            return null;
        }
    }
}
