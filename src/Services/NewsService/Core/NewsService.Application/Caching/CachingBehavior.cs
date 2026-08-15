using MediatR;
using Microsoft.Extensions.Logging;
using NewsService.Application.Interfaces;

namespace NewsService.Application.Caching;

/// <summary>
/// <see cref="ICacheableQuery"/> uygulayan sorgular için cache-aside uygular:
/// önbellekte varsa oradan döner, yoksa handler'ı çalıştırıp sonucu yazar.
///
/// Pipeline'da <c>ValidationBehavior</c>'dan <b>sonra</b> kaydedilir; böylece
/// geçersiz istekler önbelleğe hiç ulaşmaz.
/// </summary>
public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : class
{
    private readonly ICacheService _cache;
    private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;

    public CachingBehavior(ICacheService cache, ILogger<CachingBehavior<TRequest, TResponse>> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        return request switch
        {
            ICacheableQuery query => await HandleStringAsync(query, next, cancellationToken),
            IHashCacheableQuery query => await HandleHashAsync(query, next, cancellationToken),
            _ => await next()
        };
    }

    private async Task<TResponse> HandleStringAsync(
        ICacheableQuery query,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var cached = await _cache.GetAsync<TResponse>(query.CacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Önbellekten döndü: {Key}", query.CacheKey);
            return cached;
        }

        var response = await next();
        await _cache.SetAsync(query.CacheKey, response, query.Duration, cancellationToken);

        return response;
    }

    private async Task<TResponse> HandleHashAsync(
        IHashCacheableQuery query,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Sorgu kendisi "bunu önbellekleme" diyebilir (ör. arama sonuçları:
        // anahtar uzayı sınırsız, isabet oranı düşük).
        if (string.IsNullOrEmpty(query.Field))
            return await next();

        var cached = await _cache.GetHashFieldAsync<TResponse>(query.HashKey, query.Field, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Önbellekten döndü: {Key} / {Field}", query.HashKey, query.Field);
            return cached;
        }

        var response = await next();
        await _cache.SetHashFieldAsync(query.HashKey, query.Field, response, query.Duration, cancellationToken);

        return response;
    }
}
