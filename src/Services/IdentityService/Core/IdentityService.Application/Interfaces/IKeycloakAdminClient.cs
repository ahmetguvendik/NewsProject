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
