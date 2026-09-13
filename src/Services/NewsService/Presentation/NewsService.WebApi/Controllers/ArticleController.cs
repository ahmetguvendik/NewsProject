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
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // [AllowAnonymous] token'sız erişime izin verir ama geçerli bir token varsa
        // rol claim'leri yine de doluyor — taslakları yalnızca editor/admin görebilir.
        var includeUnpublished = User.IsInRole("editor") || User.IsInRole("admin");
        var result = await _mediator.Send(new GetAllArticlesQuery
        {
            IncludeUnpublished = includeUnpublished,
            Category = category,
            Search = search,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var includeUnpublished = User.IsInRole("editor") || User.IsInRole("admin");
        var result = await _mediator.Send(new GetArticleByIdQuery { Id = id, IncludeUnpublished = includeUnpublished }, cancellationToken);
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

    /// <summary>
    /// Rol kapıyı açıyor, asıl kural handler'da: admin her haberi düzenleyebilir,
    /// editör yalnızca KENDİ yazdığı ve HENÜZ YAYINLANMAMIŞ haberi.
    ///
    /// Kuralın burada değil handler'da olmasının sebebi, kaydın kendisine bakmayı
    /// gerektirmesi — yazar kim, yayında mı. [Authorize] yalnızca token'a bakabilir.
    /// </summary>
    [Authorize(Roles = "editor,admin")]
    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateArticleCommand command, CancellationToken cancellationToken)
    {
        command.EditorKeycloakId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw UnauthorizedException.MissingClaim("sub");

        command.EditorIsAdmin = User.IsInRole("admin");

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Silme yalnızca admin'de.
    ///
    /// Editörde değil, çünkü yayına alma kararı zaten admin'in — yayından
    /// kaldırmak da en az o kadar ağır bir karar. Ayrıca silme yumuşak olsa da
    /// (IsDeleted) geri getirecek bir ekran yok; yanlışlıkla silinen bir haber
    /// pratikte veritabanına elle girmeyi gerektiriyor.
    /// </summary>
    [Authorize(Roles = "admin")]
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
