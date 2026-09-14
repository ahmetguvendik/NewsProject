using MediatR;
using Microsoft.AspNetCore.Mvc;
using NewsService.Application.Features.Queries.Market.Request;
using NewsService.Application.Features.Queries.Market.Response;

namespace NewsService.WebApi.Controllers;

/// <summary>
/// Piyasa şeridi (BIST 100, dolar, euro, gram altın).
///
/// Hava durumuyla aynı gerekçeyle burada: haber alanına ait bir veri değil, dış
/// bir servisin önbelleklenmiş yansıması — ve önbellek altyapısı bu serviste.
///
/// ÇAĞRI SUNUCUDAN YAPILIYOR, tarayıcıdan değil: her ziyaretçinin doğrudan
/// Yahoo'ya dört istek atması hem rate limit riski hem de dış servisi
/// uygulamanın önüne koymak olurdu. Sunucudan çağırınca ziyaretçi sayısından
/// bağımsız olarak dakikada bir tur atılıyor.
///
/// Anonim erişime açık: şerit giriş yapmamış ziyaretçilerde de görünüyor.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MarketController : ControllerBase
{
    private readonly IMediator _mediator;

    public MarketController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<GetMarketResponse>> Get(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetMarketQuery(), cancellationToken));
}
