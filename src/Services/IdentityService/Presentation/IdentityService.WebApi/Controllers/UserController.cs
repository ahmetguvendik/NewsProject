using System.Security.Claims;
using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Features.Queries.User.Request;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Exceptions;

namespace IdentityService.WebApi.Controllers;

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

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetMyProfileQuery { KeycloakId = CurrentKeycloakId() }, cancellationToken);
        return Ok(result);
    }

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
    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        [FromBody] UpdateUserRoleCommand command,
        CancellationToken cancellationToken)
    {
        command.UserId = id;
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeactivateUserCommand { UserId = id, ActorKeycloakId = CurrentKeycloakId() }, cancellationToken);
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
