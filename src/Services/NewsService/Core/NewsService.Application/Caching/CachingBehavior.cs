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
        if (request is not ICacheableQuery cacheable)
            return await next();

        var cached = await _cache.GetAsync<TResponse>(cacheable.CacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Önbellekten döndü: {Key}", cacheable.CacheKey);
            return cached;
        }

        var response = await next();

        await _cache.SetAsync(cacheable.CacheKey, response, cacheable.Duration, cancellationToken);

        return response;
    }
}
