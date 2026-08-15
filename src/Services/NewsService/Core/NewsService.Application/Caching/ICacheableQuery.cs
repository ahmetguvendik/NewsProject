namespace NewsService.Application.Caching;

/// <summary>
/// Bir sorgunun önbelleklenmesini istediğini bildirir. <see cref="CachingBehavior{TRequest,TResponse}"/>
/// yalnızca bu arayüzü uygulayan istekleri ele alır; diğerleri pipeline'dan
/// dokunulmadan geçer.
///
/// Anahtarı sorgunun kendisi ürettiği için, hangi verinin ne kadar süreyle
/// önbelleklendiği sorgu tanımına bakılarak görülebilir.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// Önbellek anahtarı. Yanıtı değiştiren <b>her</b> parametreyi içermelidir —
    /// eksik bir boyut, bir kullanıcının yanıtının başkasına dönmesine yol açar.
    /// </summary>
    string CacheKey { get; }

    TimeSpan Duration { get; }
}
