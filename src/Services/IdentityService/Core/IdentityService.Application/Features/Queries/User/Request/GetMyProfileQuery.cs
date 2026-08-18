using IdentityService.Application.Caching;
using IdentityService.Application.Features.Queries.User.Response;
using MediatR;

namespace IdentityService.Application.Features.Queries.User.Request;

/// <summary>KeycloakId token'dan gelir — kullanıcı yalnızca kendi profilini okuyabilir.</summary>
public class GetMyProfileQuery : IRequest<MyProfileResponse>, ICacheableQuery
{
    public string KeycloakId { get; set; } = string.Empty;

    /// <summary>
    /// Anahtarda Keycloak ID zorunlu: yanıt e-posta ve aktiflik gibi kişiye özel
    /// alanlar taşıyor, tek bir ortak anahtar kullanılsaydı bir kullanıcının
    /// profili diğerine servis edilirdi.
    /// </summary>
    public string CacheKey => CacheKeys.Profile(KeycloakId);

    /// <summary>
    /// Profil, kendisini değiştiren her komutta düşürülüyor; TTL yalnızca
    /// kaçırılan bir geçersizleştirmeye karşı üst sınır.
    /// </summary>
    public TimeSpan Duration => TimeSpan.FromMinutes(30);
}
