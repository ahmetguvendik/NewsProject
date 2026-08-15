using MediatR;
using Microsoft.AspNetCore.Mvc;
using NewsService.Application.Features.Queries.Weather.Request;
using NewsService.Application.Features.Queries.Weather.Response;

namespace NewsService.WebApi.Controllers;

/// <summary>
/// Hava durumu haber alanına ait bir veri değil; dış bir servisin (Open-Meteo)
/// önbelleklenmiş yansıması. Ayrı bir servis açmak yerine burada duruyor —
/// önbellek altyapısı zaten bu serviste.
///
/// Anonim erişime açık: widget her sayfada, giriş yapmamış ziyaretçilerde de görünüyor.
/// </summary>
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
