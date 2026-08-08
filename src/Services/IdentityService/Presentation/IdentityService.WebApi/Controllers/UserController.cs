using System.Security.Claims;
using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Features.Queries.User.Request;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Exceptions;

namespace IdentityService.WebApi.Controllers;

// Sınıf seviyesinde yalnızca "giriş yapmış olma" şartı var; rol kısıtı her
// action'da ayrı ayrı belirtiliyor. Böylece kullanıcının kendi hesabını
// yönettiği uçlar (/me) admin olmadan da çalışabiliyor.
// Bir action'da yetki attribute'u unutulursa uç açıkta kalmaz, en azından
// kimlik doğrulaması ister.
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;

    public UserController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    [Authorize(Roles = "admin")]
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAllUsersQuery { Page = page, PageSize = pageSize }, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "admin")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserByIdQuery { Id = id }, cancellationToken);
        return Ok(result);
    }

    // ─── Kullanıcının kendi hesabı ──────────────────────────────────
    // Rol şartı yok: giriş yapmış her kullanıcı yalnızca KENDİ kaydına
    // erişir — hedef kullanıcı body'den değil, token'ın "sub" claim'inden
    // belirlenir, dolayısıyla başkasının kaydına dokunulamaz.

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyProfileQuery { KeycloakId = CurrentKeycloakId() }, cancellationToken);
        return Ok(result);
    }

    /// <summary>Bülten aboneliğini açar/kapatır (opt-in).</summary>
    [HttpPut("me/subscription")]
    public async Task<IActionResult> UpdateMySubscription(
        [FromBody] UpdateSubscriptionCommand command,
        CancellationToken cancellationToken)
    {
        command.KeycloakId = CurrentKeycloakId();
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    // ─── Herkese açık / servisler arası ─────────────────────────────

    /// <summary>
    /// Diğer servislerin elindeki Keycloak ID'lerini görünen isme çevirir
    /// (ör. NewsService'in makale yazarını göstermesi için).
    /// Bilerek admin dışına açık — yalnızca ad-soyad döner, hassas alan yok.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("directory")]
    public async Task<IActionResult> GetDirectory([FromQuery] string ids, CancellationToken cancellationToken)
    {
        var keycloakIds = (ids ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToList();

        var result = await _mediator.Send(new GetUserDirectoryQuery { KeycloakIds = keycloakIds }, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Servisler arası dahili çağrı — e-posta gibi hassas veri döndürdüğü için JWT
    /// yerine paylaşılan bir anahtarla (Internal:ApiKey) korunuyor. Şu an yalnızca
    /// notification-inbox-worker'ın yayın bildirimi için yazarın gerçek e-postasını
    /// çözmesinde kullanılıyor; bilerek /directory'den ayrı tutuldu.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("internal/contact")]
    public async Task<IActionResult> GetContact(
        [FromQuery] string keycloakId,
        [FromHeader(Name = "X-Internal-Api-Key")] string? apiKey,
        CancellationToken cancellationToken)
    {
        if (!IsInternalCallAuthorized(apiKey)) return Unauthorized();

        var result = await _mediator.Send(new GetUserContactQuery { KeycloakId = keycloakId }, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Bültene abone, aktif kullanıcıların iletişim bilgileri. Yayın bildirimi
    /// gönderilirken notification-inbox-worker tarafından çağrılır.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("internal/subscribers")]
    public async Task<IActionResult> GetSubscribers(
        [FromHeader(Name = "X-Internal-Api-Key")] string? apiKey,
        CancellationToken cancellationToken)
    {
        if (!IsInternalCallAuthorized(apiKey)) return Unauthorized();

        var result = await _mediator.Send(new GetSubscribersQuery(), cancellationToken);
        return Ok(result);
    }

    // ─── Admin işlemleri ────────────────────────────────────────────

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "admin")]
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteUserCommand { Id = id }, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpPost("roles")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{userId:guid}/roles/{roleName}")]
    public async Task<IActionResult> RemoveRole(Guid userId, string roleName, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveRoleCommand { UserId = userId, RoleName = roleName }, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Delete'ten farklı: kullanıcı listede kalır, yalnızca login edemez hale gelir
    /// (Keycloak enabled=false). Reversible — bkz. Activate.
    /// </summary>
    [Authorize(Roles = "admin")]
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateUserCommand { UserId = id }, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new ActivateUserCommand { UserId = id }, cancellationToken);
        return NoContent();
    }

    // ─── Yardımcılar ────────────────────────────────────────────────

    private string CurrentKeycloakId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw UnauthorizedException.MissingClaim("sub");

    private bool IsInternalCallAuthorized(string? apiKey)
    {
        var expectedKey = _configuration["Internal:ApiKey"];
        // Anahtar hiç yapılandırılmamışsa varsayılan olarak REDDET —
        // yanlış yapılandırma güvenlik açığına dönüşmesin.
        return !string.IsNullOrEmpty(expectedKey) && apiKey == expectedKey;
    }
}
