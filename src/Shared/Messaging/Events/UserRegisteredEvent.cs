namespace Shared.Messaging.Events;

public class UserRegisteredEvent
{
    public Guid UserId { get; set; }
    public string KeycloakId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
}
