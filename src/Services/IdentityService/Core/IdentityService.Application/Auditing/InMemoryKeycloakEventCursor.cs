using IdentityService.Application.Interfaces;

namespace IdentityService.Application.Auditing;

// Tek örnek varsayar ve yeniden başlatmada sıfırlanır; servis çoğaltılırsa Redis'e taşınmalı.
public sealed class InMemoryKeycloakEventCursor : IKeycloakEventCursor
{
    private long? _lastSeenTime;

    public ValueTask<long?> ReadAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(_lastSeenTime);

    public ValueTask WriteAsync(long time, CancellationToken cancellationToken = default)
    {
        _lastSeenTime = time;
        return ValueTask.CompletedTask;
    }
}
