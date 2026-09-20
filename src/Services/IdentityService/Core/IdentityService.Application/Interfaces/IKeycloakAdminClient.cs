namespace IdentityService.Application.Interfaces;

public interface IKeycloakAdminClient
{
    Task<string> CreateUserAsync(string email, string password, string firstName, string lastName, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(string keycloakId, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(string keycloakId, string roleName, CancellationToken cancellationToken = default);
    Task RemoveRoleAsync(string keycloakId, string roleName, CancellationToken cancellationToken = default);
    Task DisableUserAsync(string keycloakId, CancellationToken cancellationToken = default);
    Task EnableUserAsync(string keycloakId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kullanıcıya e-posta doğrulama bağlantısı gönderir.
    ///
    /// Realm'de <c>verifyEmail</c> açıkken doğrulanmamış hesap giriş YAPAMIYOR
    /// (direct grant <c>invalid_grant / Account is not fully set up</c> döner).
    /// Dolayısıyla bu çağrı kaydın isteğe bağlı bir süsü değil, zorunlu adımı:
    /// gönderilmezse kullanıcı hesabını hiçbir zaman kullanamaz.
    /// </summary>
    Task SendVerificationEmailAsync(string keycloakId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Keycloak'ın kaydettiği son güvenlik olaylarını, yenisi önce gelecek şekilde döndürür.
    /// </summary>
    /// <param name="types">
    /// Okunacak olay türleri (SEND_RESET_PASSWORD, RESET_PASSWORD…). Boş bırakılırsa
    /// Keycloak'ın kayıt açık olan tüm türleri gelir.
    /// </param>
    /// <param name="max">
    /// Tek çağrıda getirilecek azami kayıt. Çağıran taraf en son gördüğü zamandan
    /// yenilerini süzüyor; bu sınır, iki yoklama arasında bu sayıdan fazla olay
    /// olursa en eskilerinin atlanacağı anlamına gelir.
    /// </param>
    Task<IReadOnlyList<KeycloakSecurityEvent>> GetSecurityEventsAsync(
        IReadOnlyCollection<string> types,
        int max,
        CancellationToken cancellationToken = default);
}
