namespace IdentityService.Application.Features.Commands.User.Response;

public class CreateUserResponse
{
    public Guid Id { get; set; }
    public string KeycloakId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
