using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsService.Application.Features.Commands.Tag.Request;
using NewsService.Application.Features.Queries.Tag.Request;

namespace NewsService.WebApi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TagController : ControllerBase
{
    private readonly IMediator _mediator;

    public TagController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetAllTagsQuery(), cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTagByIdQuery { Id = id }, cancellationToken);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTagCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "admin")]
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateTagCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteTagCommand { Id = id }, cancellationToken);
        return NoContent();
    }
}
