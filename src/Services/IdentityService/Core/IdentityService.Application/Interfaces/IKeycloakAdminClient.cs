namespace IdentityService.Application.Interfaces;

public interface IKeycloakAdminClient
{
    Task<string> CreateUserAsync(string email, string password, string firstName, string lastName, CancellationToken cancellationToken = default);
}
