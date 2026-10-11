namespace IdentityService.Application.Interfaces;

// Yazamazsa hata fırlatmaz, loglar: Redis kesintisi yönetim işlemlerini durdurmasın.
public interface ITokenRevocationStore
{
    Task RevokeAsync(string keycloakId, string reason, CancellationToken cancellationToken = default);
}
