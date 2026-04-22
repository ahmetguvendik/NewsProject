namespace IdentityService.Application.Interfaces;

public interface IKeycloakAdminClient
{
    Task<string> CreateUserAsync(string email, string password, string firstName, string lastName, CancellationToken cancellationToken = default);
    Task DeleteUserAsync(string keycloakId, CancellationToken cancellationToken = default);
    Task AssignRoleAsync(string keycloakId, string roleName, CancellationToken cancellationToken = default);
    Task RemoveRoleAsync(string keycloakId, string roleName, CancellationToken cancellationToken = default);
    Task DisableUserAsync(string keycloakId, CancellationToken cancellationToken = default);
}
