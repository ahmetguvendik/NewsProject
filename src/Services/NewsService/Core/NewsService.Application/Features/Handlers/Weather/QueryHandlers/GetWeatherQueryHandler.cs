using MediatR;
using NewsService.Application.Features.Queries.Weather.Request;
using NewsService.Application.Features.Queries.Weather.Response;
using NewsService.Application.Interfaces;

namespace NewsService.Application.Features.Handlers.Weather.QueryHandlers;

public class GetWeatherQueryHandler : IRequestHandler<GetWeatherQuery, GetWeatherResponse>
{
    private readonly IWeatherClient _weatherClient;

    public GetWeatherQueryHandler(IWeatherClient weatherClient) => _weatherClient = weatherClient;

    public Task<GetWeatherResponse> Handle(GetWeatherQuery request, CancellationToken cancellationToken)
        => _weatherClient.GetCurrentAsync(cancellationToken);
}
