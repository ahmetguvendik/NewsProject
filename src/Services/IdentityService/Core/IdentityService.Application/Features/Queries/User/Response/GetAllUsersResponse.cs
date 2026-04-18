namespace IdentityService.Application.Features.Queries.User.Response;

public class GetAllUsersResponse
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; }
    public List<string> Roles { get; set; } = [];
}
