using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsService.Application.Features.Commands.Article.Request;
using NewsService.Application.Features.Queries.Article.Request;
using Shared.Exceptions;

namespace NewsService.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ArticleController : ControllerBase
{
    private readonly IMediator _mediator;

    public ArticleController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllArticlesQuery(), cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetArticleByIdQuery { Id = id }, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "editor,admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateArticleCommand command, CancellationToken cancellationToken)
    {
        var keycloakId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw UnauthorizedException.MissingClaim("sub");

        command.AuthorKeycloakId = keycloakId;

        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "editor,admin")]
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateArticleCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "editor,admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteArticleCommand { Id = id }, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "admin")]
    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new PublishArticleCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
