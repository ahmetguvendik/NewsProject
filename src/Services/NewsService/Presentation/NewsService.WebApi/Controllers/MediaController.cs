using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsService.Application.Interfaces;
using NewsService.Application.Media;

namespace NewsService.WebApi.Controllers;

/// <summary>
/// Medya yükleme akışı iki adımdır:
/// <list type="number">
/// <item><c>upload-url</c> — izin verilirse imzalı bir yükleme adresi döner.</item>
/// <item><c>commit</c> — client doğrudan depoya yükledikten sonra dosya doğrulanıp kalıcılaştırılır.</item>
/// </list>
/// Dosyanın kendisi bu servise hiç uğramaz.
/// </summary>
[ApiController]
[Authorize(Roles = "editor,admin")]
[Route("api/media")]
public class MediaController : ControllerBase
{
    private readonly IStorageService _storage;

    public MediaController(IStorageService storage) => _storage = storage;

    /// <summary>İzinli tip/boyut için kısa ömürlü imzalı yükleme adresi üretir.</summary>
    [HttpPost("upload-url")]
    public ActionResult<PresignedUpload> CreateUploadUrl([FromBody] UploadUrlRequest request)
        => Ok(_storage.CreateUploadUrl(request.ContentType, request.SizeBytes));

    /// <summary>
    /// Yüklenen dosyayı gerçek içeriğine bakarak doğrular ve kalıcı alana taşır.
    /// Dönen <c>key</c> makalenin görsel alanına yazılacak değerdir.
    /// </summary>
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

    /// <summary>Client'ın yükleme öncesi kuralları gösterebilmesi için politika.</summary>
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
