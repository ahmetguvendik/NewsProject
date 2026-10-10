namespace NewsService.Application.Interfaces;

public interface IStorageService
{
    PresignedUpload CreateUploadUrl(string contentType, long sizeBytes);

    Task<string> CommitAsync(string stagingKey, CancellationToken cancellationToken = default);

    string? ResolvePublicUrl(string? storedValue);

    string? ResolveSrcset(string? storedValue);
}

public record PresignedUpload(string UploadUrl, string Key, string ContentType, int ExpiresInSeconds);
