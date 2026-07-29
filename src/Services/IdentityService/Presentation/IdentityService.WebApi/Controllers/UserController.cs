using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Features.Queries.User.Request;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.WebApi.Controllers;

[Authorize(Roles = "admin")]
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
    
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllUsersQuery(), cancellationToken);
        return Ok(result);
    }
    
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserByIdQuery { Id = id }, cancellationToken);
        return Ok(result);
    }

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
        var expectedKey = _configuration["Internal:ApiKey"];
        if (string.IsNullOrEmpty(expectedKey) || apiKey != expectedKey)
            return Unauthorized();

        var result = await _mediator.Send(new GetUserContactQuery { KeycloakId = keycloakId }, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }
    
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
    
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteUserCommand { Id = id }, cancellationToken);
        return NoContent();
    }


    [HttpPost("roles")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleCommand command, CancellationToken cancellationToken)
    {
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{userId:guid}/roles/{roleName}")]
    public async Task<IActionResult> RemoveRole(Guid userId, string roleName, CancellationToken cancellationToken)
    {
        await _mediator.Send(new RemoveRoleCommand { UserId = userId, RoleName = roleName }, cancellationToken);
        return NoContent();
    }
}
