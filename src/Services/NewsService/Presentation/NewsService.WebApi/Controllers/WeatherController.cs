using MediatR;
using Microsoft.AspNetCore.Mvc;
using NewsService.Application.Features.Queries.Weather.Request;
using NewsService.Application.Features.Queries.Weather.Response;

namespace NewsService.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeatherController : ControllerBase
{
    private readonly IMediator _mediator;

    public WeatherController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<GetWeatherResponse>> Get(CancellationToken cancellationToken)
        => Ok(await _mediator.Send(new GetWeatherQuery(), cancellationToken));
}
