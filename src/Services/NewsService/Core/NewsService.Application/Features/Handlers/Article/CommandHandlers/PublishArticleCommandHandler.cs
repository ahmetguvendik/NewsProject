using MediatR;
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

    public PublishArticleCommandHandler(
        IGenericRepository<Domain.Entities.Article> articleRepository,
        IEventPublisher eventPublisher,
        IUnitOfWork unitOfWork)
    {
        _articleRepository = articleRepository;
        _eventPublisher = eventPublisher;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(PublishArticleCommand request, CancellationToken cancellationToken)
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
            PublishedAt = article.PublishedAt!.Value
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
