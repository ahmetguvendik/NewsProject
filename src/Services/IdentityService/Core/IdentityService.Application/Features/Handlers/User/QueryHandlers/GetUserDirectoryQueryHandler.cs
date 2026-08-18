using IdentityService.Application.Caching;
using IdentityService.Application.Features.Queries.User.Request;
using IdentityService.Application.Features.Queries.User.Response;
using IdentityService.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Application.Features.Handlers.User.QueryHandlers;

/// <summary>
/// Önbelleği <see cref="ICacheService"/> üzerinden doğrudan kullanır,
/// <c>ICacheableQuery</c> ile pipeline'a bırakmaz. Sebebi: sorgu keyfi bir ID
/// listesi alıyor. İstek bütün olarak anahtarlansaydı (<c>ids=a,b,c</c>)
/// anahtar uzayı kombinasyon sayısıyla büyür, isabet oranı çökerdi — akış
/// listesinde arama sorgularının önbelleklenmeme gerekçesinin aynısı.
///
/// Bunun yerine önbellek <b>kullanıcı başına</b> tutuluyor: tek bir hash'in
/// alanları. Aynı yazar yüzlerce haberde geçtiği için isabet oranı yüksek,
/// ve bir kullanıcı değiştiğinde yalnızca onun alanı düşürülüyor.
/// </summary>
public class GetUserDirectoryQueryHandler : IRequestHandler<GetUserDirectoryQuery, List<UserDirectoryEntryResponse>>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly ICacheService _cache;

    /// <summary>
    /// Ad-soyad neredeyse hiç değişmiyor ve değiştiğinde zaten geçersizleştiriliyor;
    /// TTL yalnızca kaçırılan bir geçersizleştirmeye karşı üst sınır.
    /// </summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(6);

    public GetUserDirectoryQueryHandler(
        IGenericRepository<Domain.Entities.User> userRepository,
        ICacheService cache)
    {
        _userRepository = userRepository;
        _cache = cache;
    }

    public async Task<List<UserDirectoryEntryResponse>> Handle(GetUserDirectoryQuery request, CancellationToken cancellationToken)
    {
        if (request.KeycloakIds.Count == 0) return [];

        var names = new Dictionary<string, string>(request.KeycloakIds.Count);

        foreach (var (keycloakId, displayName) in
                 await _cache.GetHashFieldsAsync<string>(CacheKeys.Directory, request.KeycloakIds, cancellationToken))
        {
            names[keycloakId] = displayName;
        }

        var missing = request.KeycloakIds.Where(id => !names.ContainsKey(id)).ToList();

        if (missing.Count > 0)
        {
            // Bulunamayan ID'ler (ör. silinmiş kullanıcı) sessizce atlanır — çağıran
            // taraf eksik girişleri kendi fallback'iyle (kısaltılmış ID vb.) gösterir.
            // Bu "yok" bilgisi önbelleğe yazılmıyor: sıcak yol makale yazarını
            // çözmek ve yazarlar var, dolayısıyla ıska nadir. Negatif önbellek
            // eklemek, henüz oluşturulmamış kullanıcıyı yokmuş gibi dondurma
            // riskini karşılığında çok az kazançla getirirdi.
            var fetched = await _userRepository.GetQueryable()
                .Where(u => missing.Contains(u.KeycloakId))
                .Select(u => new
                {
                    u.KeycloakId,
                    DisplayName = (u.FirstName + " " + u.LastName).Trim()
                })
                .ToDictionaryAsync(row => row.KeycloakId, row => row.DisplayName, cancellationToken);

            await _cache.SetHashFieldsAsync(CacheKeys.Directory, fetched, CacheDuration, cancellationToken);

            foreach (var (keycloakId, displayName) in fetched)
                names[keycloakId] = displayName;
        }

        return names
            .Select(entry => new UserDirectoryEntryResponse
            {
                KeycloakId = entry.Key,
                DisplayName = entry.Value
            })
            .ToList();
    }
}
