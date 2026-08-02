using MediatR;
using NewsService.Application.Features.Queries.Article.Response;
using Shared.Models;

namespace NewsService.Application.Features.Queries.Article.Request;

public class GetAllArticlesQuery : IRequest<PagedResult<GetAllArticlesResponse>>
{
    /// <summary>Yalnızca editor/admin için true — taslaklar da listeye dahil edilir.</summary>
    public bool IncludeUnpublished { get; set; }

    /// <summary>Verilirse yalnızca bu kategori adına sahip makaleler döner.</summary>
    public string? Category { get; set; }

    /// <summary>Verilirse başlık/özette alt-dize eşleşmesi aranır (case-insensitive).</summary>
    public string? Search { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
