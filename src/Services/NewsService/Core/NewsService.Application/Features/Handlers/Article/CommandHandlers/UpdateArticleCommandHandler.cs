using MediatR;
using NewsService.Application.Caching;
using NewsService.Application.Features.Commands.Article.Request;
using NewsService.Application.Features.Commands.Article.Response;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;
using NewsService.Domain.Entities;
using Shared.Exceptions;

namespace NewsService.Application.Features.Handlers.Article.CommandHandlers;

public class UpdateArticleCommandHandler : IRequestHandler<UpdateArticleCommand, UpdateArticleResponse>
{
    private readonly IGenericRepository<Domain.Entities.Article> _articleRepository;
    private readonly IGenericRepository<Domain.Entities.Category> _categoryRepository;
    private readonly IGenericRepository<Domain.Entities.Tag> _tagRepository;
    private readonly IArticleTagRepository _articleTagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public UpdateArticleCommandHandler(
        IGenericRepository<Domain.Entities.Article> articleRepository,
        IGenericRepository<Domain.Entities.Category> categoryRepository,
        IGenericRepository<Domain.Entities.Tag> tagRepository,
        IArticleTagRepository articleTagRepository,
        IUnitOfWork unitOfWork, ICacheService cache)
    {
        _articleRepository = articleRepository;
        _categoryRepository = categoryRepository;
        _tagRepository = tagRepository;
        _articleTagRepository = articleTagRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<UpdateArticleResponse> Handle(UpdateArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _articleRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.Article(request.Id);

        // Yeni CategoryId veritabanında var mı?
        _ = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw NotFoundException.Category(request.CategoryId);

        // Gönderilen tag'lerin hepsi var mı? (Create ile aynı doğrulama)
        var requestedTagIds = request.TagIds.Distinct().ToList();
        foreach (var tagId in requestedTagIds)
        {
            _ = await _tagRepository.GetByIdAsync(tagId, cancellationToken)
                ?? throw NotFoundException.Tag(tagId);
        }

        article.Title = request.Title;
        article.Content = request.Content;
        article.Summary = request.Summary;
        article.ImageUrl = request.ImageUrl;
        article.CategoryId = request.CategoryId;
        article.UpdatedAt = DateTime.UtcNow;

        await _articleRepository.UpdateAsync(article, cancellationToken);
        await SyncTagsAsync(article.Id, requestedTagIds, cancellationToken);

        // Makale alanları + etiket değişiklikleri tek transaction'da kaydedilir
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Listenin tüm varyantları (sayfa, kategori, rol) tek anahtarda;
        // biri değiştiğinde hepsi bayatladığı için tamamı düşürülüyor.
        await _cache.RemoveAsync(CacheKeys.ArticleLists, cancellationToken);

        return new UpdateArticleResponse
        {
            Id = article.Id,
            Title = article.Title,
            Summary = article.Summary,
            CategoryId = article.CategoryId,
            UpdatedAt = article.UpdatedAt
        };
    }

    /// <summary>
    /// Mevcut etiketlerle istenen kümeyi karşılaştırıp yalnızca farkı uygular:
    /// hepsini silip yeniden eklemek yerine, değişmeyen bağlantılara dokunulmaz.
    /// </summary>
    private async Task SyncTagsAsync(Guid articleId, List<Guid> requestedTagIds, CancellationToken cancellationToken)
    {
        var existing = await _articleTagRepository.GetByArticleIdAsync(articleId, cancellationToken);
        var existingTagIds = existing.Select(link => link.TagId).ToHashSet();
        var requested = requestedTagIds.ToHashSet();

        foreach (var removed in existing.Where(link => !requested.Contains(link.TagId)))
            await _articleTagRepository.DeleteAsync(removed, cancellationToken);

        foreach (var addedTagId in requested.Where(tagId => !existingTagIds.Contains(tagId)))
        {
            await _articleTagRepository.CreateAsync(
                new ArticleTag { ArticleId = articleId, TagId = addedTagId },
                cancellationToken);
        }
    }
}
