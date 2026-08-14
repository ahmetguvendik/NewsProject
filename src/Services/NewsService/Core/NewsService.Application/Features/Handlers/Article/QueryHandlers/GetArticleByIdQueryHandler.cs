using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsService.Application.Features.Queries.Article.Request;
using NewsService.Application.Features.Queries.Article.Response;
using NewsService.Application.Interfaces;
using Shared.Exceptions;

namespace NewsService.Application.Features.Handlers.Article.QueryHandlers;

public class GetArticleByIdQueryHandler : IRequestHandler<GetArticleByIdQuery, GetArticleByIdResponse>
{
    private readonly IGenericRepository<Domain.Entities.Article> _articleRepository;
    private readonly IStorageService _storage;

    public GetArticleByIdQueryHandler(
        IGenericRepository<Domain.Entities.Article> articleRepository,
        IStorageService storage)
    {
        _articleRepository = articleRepository;
        _storage = storage;
    }

    public async Task<GetArticleByIdResponse> Handle(GetArticleByIdQuery request, CancellationToken cancellationToken)
    {
        var article = await _articleRepository.GetQueryable()
            .Include(a => a.Category)
            .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
            .Where(a => a.Id == request.Id && !a.IsDeleted && (request.IncludeUnpublished || a.IsPublished))
            .Select(a => new GetArticleByIdResponse
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                Summary = a.Summary,
                ImageUrl = a.ImageUrl,
                ImageKey = a.ImageUrl,
                AuthorKeycloakId = a.AuthorKeycloakId,
                CategoryId = a.CategoryId,
                CategoryName = a.Category.Name,
                IsPublished = a.IsPublished,
                PublishedAt = a.PublishedAt,
                CreatedAt = a.CreatedAt,
                Tags = a.ArticleTags.Select(at => at.Tag.Name).ToList(),
                TagIds = a.ArticleTags.Select(at => at.TagId).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (article is null)
            throw NotFoundException.Article(request.Id);

        // ImageUrl görüntülemek için çözümlenir; ImageKey ham haliyle kalır ki
        // düzenleme formu geri gönderdiğinde anahtar tam URL'e dönüşmesin.
        article.ImageUrl = _storage.ResolvePublicUrl(article.ImageUrl);

        return article;
    }
}
