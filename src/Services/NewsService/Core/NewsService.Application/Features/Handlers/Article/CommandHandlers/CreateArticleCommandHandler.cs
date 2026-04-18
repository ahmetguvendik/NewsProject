using MediatR;
using NewsService.Application.Features.Commands.Article.Request;
using NewsService.Application.Features.Commands.Article.Response;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;
using NewsService.Domain.Entities;
using Shared.Messaging;
using Shared.Messaging.Events;

namespace NewsService.Application.Features.Handlers.Article.CommandHandlers;

public class CreateArticleCommandHandler : IRequestHandler<CreateArticleCommand, CreateArticleResponse>
{
    private readonly IGenericRepository<Domain.Entities.Article> _articleRepository;
    private readonly IArticleTagRepository _articleTagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public CreateArticleCommandHandler(
        IGenericRepository<Domain.Entities.Article> articleRepository,
        IArticleTagRepository articleTagRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher)
    {
        _articleRepository = articleRepository;
        _articleTagRepository = articleTagRepository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<CreateArticleResponse> Handle(CreateArticleCommand request, CancellationToken cancellationToken)
    {
        var article = new Domain.Entities.Article
        {
            Title = request.Title,
            Content = request.Content,
            Summary = request.Summary,
            ImageUrl = request.ImageUrl,
            AuthorKeycloakId = request.AuthorKeycloakId,
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

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(Topics.Article.Created, new ArticleCreatedEvent
        {
            ArticleId = article.Id,
            Title = article.Title,
            AuthorKeycloakId = article.AuthorKeycloakId,
            CreatedAt = article.CreatedAt
        }, cancellationToken);

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
