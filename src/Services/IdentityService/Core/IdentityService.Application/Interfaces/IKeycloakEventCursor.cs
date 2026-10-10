namespace IdentityService.Application.Interfaces;

public interface IKeycloakEventCursor
{
    ValueTask<long?> ReadAsync(CancellationToken cancellationToken = default);

    ValueTask WriteAsync(long time, CancellationToken cancellationToken = default);
}
