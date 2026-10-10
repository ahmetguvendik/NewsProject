using MediatR;
using Microsoft.AspNetCore.Mvc;
using NewsService.Application.Features.Queries.Market.Request;
using NewsService.Application.Features.Queries.Market.Response;

namespace NewsService.WebApi.Controllers;

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
