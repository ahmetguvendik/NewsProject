namespace IdentityService.Application.Interfaces;

// Önbellek değil güvenlik kaydı: RevokeAsync yazamazsa hata fırlatır.
public interface ITokenRevocationStore
{
    Task RevokeAsync(string keycloakId, string reason, CancellationToken cancellationToken = default);
}
