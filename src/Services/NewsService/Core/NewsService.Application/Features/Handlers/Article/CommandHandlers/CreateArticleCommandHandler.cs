using MediatR;
using NewsService.Application.Caching;
using NewsService.Application.Features.Commands.Article.Request;
using NewsService.Application.Features.Commands.Article.Response;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;
using NewsService.Domain.Entities;
using Shared.Exceptions;

namespace NewsService.Application.Features.Handlers.Article.CommandHandlers;

public class CreateArticleCommandHandler : IRequestHandler<CreateArticleCommand, CreateArticleResponse>
{
    private readonly IGenericRepository<Domain.Entities.Article> _articleRepository;
    private readonly IGenericRepository<Domain.Entities.Category> _categoryRepository;
    private readonly IGenericRepository<Domain.Entities.Tag> _tagRepository;
    private readonly IArticleTagRepository _articleTagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public CreateArticleCommandHandler(
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

    public async Task<CreateArticleResponse> Handle(CreateArticleCommand request, CancellationToken cancellationToken)
    {
        // Kategori var mı?
        _ = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken)
            ?? throw NotFoundException.Category(request.CategoryId);

        // Gönderilen tag'lerin hepsi var mı?
        foreach (var tagId in request.TagIds)
        {
            _ = await _tagRepository.GetByIdAsync(tagId, cancellationToken)
                ?? throw NotFoundException.Tag(tagId);
        }

        var article = new Domain.Entities.Article
        {
            Title = request.Title,
            Content = request.Content,
            Summary = request.Summary,
            ImageUrl = request.ImageUrl,
            AuthorKeycloakId = request.AuthorKeycloakId,
            NotifySubscribers = request.NotifySubscribers,
            CategoryId = request.CategoryId
        };

        await _articleRepository.CreateAsync(article, cancellationToken);

        foreach (var tagId in request.TagIds)
        {
            await _articleTagRepository.CreateAsync(new ArticleTag
            {
                ArticleId = article.Id,
                TagId = tagId
            }, cancellationToken);
        }

        // Taslak oluşturmak event yayınlamıyor: abonelere/yazara bildirim yalnızca
        // haber YAYINLANDIĞINDA (article.published) gönderiliyor.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Listenin tüm varyantları (sayfa, kategori, rol) tek anahtarda;
        // biri değiştiğinde hepsi bayatladığı için tamamı düşürülüyor.
        await _cache.RemoveAsync(CacheKeys.ArticleLists, cancellationToken);

        return new CreateArticleResponse
        {
            Id = article.Id,
            Title = article.Title,
            Summary = article.Summary,
            AuthorKeycloakId = article.AuthorKeycloakId,
            CategoryId = article.CategoryId,
            IsPublished = article.IsPublished,
            CreatedAt = article.CreatedAt
        };
    }
}
