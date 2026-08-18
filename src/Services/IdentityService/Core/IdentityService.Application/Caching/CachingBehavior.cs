using IdentityService.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentityService.Application.Caching;

/// <summary>
/// <see cref="ICacheableQuery"/> uygulayan sorgular için cache-aside uygular:
/// önbellekte varsa oradan döner, yoksa handler'ı çalıştırıp sonucu yazar.
///
/// Pipeline'da <c>ValidationBehavior</c>'dan <b>sonra</b> kaydedilir; böylece
/// geçersiz istekler önbelleğe hiç ulaşmaz.
///
/// Behavior "ya hep ya hiç" çalışır: isteğin tamamı ya önbellekten döner ya da
/// handler'a gider. Yanıtı parça parça önbellekleyen sorgular (bkz.
/// <c>GetUserDirectoryQueryHandler</c>) bu yüzden buraya bağlanmaz,
/// <see cref="ICacheService"/>'i doğrudan kullanır.
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
        if (request is not ICacheableQuery query)
            return await next();

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
}
