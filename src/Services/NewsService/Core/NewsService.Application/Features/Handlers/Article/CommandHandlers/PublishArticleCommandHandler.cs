using MediatR;
using NewsService.Application.Features.Commands.Article.Request;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;

namespace NewsService.Application.Features.Handlers.Article.CommandHandlers;

public class PublishArticleCommandHandler : IRequestHandler<PublishArticleCommand>
{
    private readonly IGenericRepository<Domain.Entities.Article> _articleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PublishArticleCommandHandler(IGenericRepository<Domain.Entities.Article> articleRepository, IUnitOfWork unitOfWork)
    {
        _articleRepository = articleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(PublishArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _articleRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new Exception($"Article not found: {request.Id}");

        article.IsPublished = true;
        article.PublishedAt = DateTime.UtcNow;
        article.UpdatedAt = DateTime.UtcNow;

        await _articleRepository.UpdateAsync(article, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
