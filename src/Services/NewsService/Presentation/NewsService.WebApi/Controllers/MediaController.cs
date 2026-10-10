using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsService.Application.Interfaces;
using NewsService.Application.Media;

namespace NewsService.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "editor,admin")]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly IStorageService _storage;

    public MediaController(IStorageService storage) => _storage = storage;

    [HttpPost("upload-url")]
    public ActionResult<PresignedUpload> CreateUploadUrl([FromBody] UploadUrlRequest request)
        => Ok(_storage.CreateUploadUrl(request.ContentType, request.SizeBytes));

    [HttpPost("commit")]
    public async Task<ActionResult<CommitUploadResponse>> Commit(
        [FromBody] CommitUploadRequest request,
        CancellationToken cancellationToken)
    {
        var key = await _storage.CommitAsync(request.Key, cancellationToken);

        return Ok(new CommitUploadResponse
        {
            Key = key,
            Url = _storage.ResolvePublicUrl(key)
        });
    }

    [HttpGet("policy")]
    [AllowAnonymous]
    public ActionResult<MediaPolicyResponse> GetPolicy() => Ok(new MediaPolicyResponse
    {
        MaxSizeBytes = MediaPolicy.MaxSizeBytes,
        AllowedContentTypes = [.. MediaPolicy.AllowedContentTypes]
    });
}

public class UploadUrlRequest
{
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}

public class CommitUploadRequest
{
    public string Key { get; set; } = string.Empty;
}

public class CommitUploadResponse
{
    public string Key { get; set; } = string.Empty;
    public string? Url { get; set; }
}

public class MediaPolicyResponse
{
    public long MaxSizeBytes { get; set; }
    public List<string> AllowedContentTypes { get; set; } = [];
}
