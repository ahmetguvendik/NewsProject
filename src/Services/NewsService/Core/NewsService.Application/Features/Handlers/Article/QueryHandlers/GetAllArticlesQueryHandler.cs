using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsService.Application.Features.Queries.Article.Request;
using NewsService.Application.Features.Queries.Article.Response;
using NewsService.Application.Interfaces;
using Shared.Models;

namespace NewsService.Application.Features.Handlers.Article.QueryHandlers;

public class GetAllArticlesQueryHandler : IRequestHandler<GetAllArticlesQuery, PagedResult<GetAllArticlesResponse>>
{
    private readonly IGenericRepository<Domain.Entities.Article> _articleRepository;
    private readonly IStorageService _storage;

    public GetAllArticlesQueryHandler(
        IGenericRepository<Domain.Entities.Article> articleRepository,
        IStorageService storage)
    {
        _articleRepository = articleRepository;
        _storage = storage;
    }

    public async Task<PagedResult<GetAllArticlesResponse>> Handle(GetAllArticlesQuery request, CancellationToken cancellationToken)
    {
        var query = _articleRepository.GetQueryable()
            .Include(a => a.Category)
            .Where(a => !a.IsDeleted && (request.IncludeUnpublished || a.IsPublished));

        if (!string.IsNullOrWhiteSpace(request.Category))
            query = query.Where(a => a.Category.Name == request.Category);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            // ToLower().Contains() → standart EF Core çevirisiyle Postgres'te
            // case-insensitive alt-dize eşleşmesi; Npgsql'e özel bir fonksiyon
            // (EF.Functions.ILike) kullanmadığı için Application katmanı
            // veritabanı sağlayıcısından bağımsız kalıyor.
            var needle = request.Search.Trim().ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(needle) ||
                (a.Summary != null && a.Summary.ToLower().Contains(needle)));
        }

        // Skip/Take'in sayfalar arasında tutarlı sonuç vermesi için deterministik
        // bir sıralama şart. Client'ın önceden kendi yaptığı sıralamayla aynı
        // mantık: yayınlanmışsa yayın tarihi, taslaksa oluşturulma tarihi.
        query = query.OrderByDescending(a => a.PublishedAt ?? a.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var articles = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new GetAllArticlesResponse
            {
                Id = a.Id,
                Title = a.Title,
                Summary = a.Summary,
                ImageUrl = a.ImageUrl,
                AuthorKeycloakId = a.AuthorKeycloakId,
                CategoryName = a.Category.Name,
                IsPublished = a.IsPublished,
                PublishedAt = a.PublishedAt,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Veritabanında depo anahtarı tutulur (bucket/CDN değişince satırlar
        // migrate edilmesin diye); dışarıya her zaman tam adres çıkar.
        foreach (var article in articles)
        {
            article.ImageSrcset = _storage.ResolveSrcset(article.ImageUrl);
            article.ImageUrl = _storage.ResolvePublicUrl(article.ImageUrl);
        }

        return new PagedResult<GetAllArticlesResponse>
        {
            Items = articles,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
