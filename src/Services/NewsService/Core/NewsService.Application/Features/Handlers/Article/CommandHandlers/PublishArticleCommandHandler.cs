using System.Data;
using MediatR;
using NewsService.Application.Caching.Concrete;
using NewsService.Application.Exceptions;
using NewsService.Application.Features.Commands.Article.Request;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;
using Shared.Exceptions;
using Shared.Messaging;
using Shared.Messaging.Events;

namespace NewsService.Application.Features.Handlers.Article.CommandHandlers;

public class PublishArticleCommandHandler : IRequestHandler<PublishArticleCommand>
{
    private readonly IGenericRepository<Domain.Entities.Article> _articleRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public PublishArticleCommandHandler(
        IGenericRepository<Domain.Entities.Article> articleRepository,
        IEventPublisher eventPublisher,
        IUnitOfWork unitOfWork, ICacheService cache)
    {
        _articleRepository = articleRepository;
        _eventPublisher = eventPublisher;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task Handle(PublishArticleCommand request, CancellationToken cancellationToken)
    {
        // Serializable: eşzamanlı iki yayın isteğinden yalnızca biri olay üretsin (çift mail gitmesin).
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        try
        {
            var article = await _articleRepository.GetByIdAsync(request.Id, cancellationToken)
                ?? throw NotFoundException.Article(request.Id);

            if (article.IsPublished)
                throw ConflictException.ArticleAlreadyPublished(request.Id);

            article.IsPublished = true;
            article.PublishedAt = DateTime.UtcNow;
            article.UpdatedAt = DateTime.UtcNow;

            await _articleRepository.UpdateAsync(article, cancellationToken);

            await _eventPublisher.PublishAsync(Topics.Article.Published, new ArticlePublishedEvent
            {
                ArticleId = article.Id,
                Title = article.Title,
                AuthorKeycloakId = article.AuthorKeycloakId,
                NotifySubscribers = article.NotifySubscribers,
                PublishedAt = article.PublishedAt!.Value
            }, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);
            throw;
        }

        // Listenin tüm varyantları (sayfa, kategori, rol) tek anahtarda;
        // biri değiştiğinde hepsi bayatladığı için tamamı düşürülüyor.
        await _cache.RemoveAsync(CacheKeys.ArticleLists, cancellationToken);
    }
}
