namespace Shared.Models;

public sealed record ErrorResponse
{
    public int Status { get; init; }

    public string ErrorCode { get; init; } = default!;

    public string Message { get; init; } = default!;

    public string Description { get; init; } = default!;

    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
