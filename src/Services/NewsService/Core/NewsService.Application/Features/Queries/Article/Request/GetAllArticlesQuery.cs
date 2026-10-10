using MediatR;
using NewsService.Application.Caching.Abstract;
using NewsService.Application.Caching.Concrete;
using NewsService.Application.Features.Queries.Article.Response;
using Shared.Models;

namespace NewsService.Application.Features.Queries.Article.Request;

public class GetAllArticlesQuery : IRequest<PagedResult<GetAllArticlesResponse>>, IHashCacheableQuery
{
    public bool IncludeUnpublished { get; set; }

    public string? Category { get; set; }

    public string? Search { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    public string HashKey => CacheKeys.ArticleLists;

    // tumu/yayin ayrımı şart: yoksa taslak gören yanıt anonime servis edilir. Arama önbelleklenmez.
    public string? Field => string.IsNullOrWhiteSpace(Search)
        ? $"{(IncludeUnpublished ? "tumu" : "yayin")}:kategori={Category ?? "-"}:sayfa={Page}:boyut={PageSize}"
        : null;

    public TimeSpan Duration => TimeSpan.FromMinutes(15);
}
