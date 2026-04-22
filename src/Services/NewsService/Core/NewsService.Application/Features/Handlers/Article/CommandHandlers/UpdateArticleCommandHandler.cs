using MediatR;
using NewsService.Application.Features.Commands.Article.Request;
using NewsService.Application.Features.Commands.Article.Response;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;
using Shared.Exceptions;

namespace NewsService.Application.Features.Handlers.Article.CommandHandlers;

public class UpdateArticleCommandHandler : IRequestHandler<UpdateArticleCommand, UpdateArticleResponse>
{
    private readonly IGenericRepository<Domain.Entities.Article> _articleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateArticleCommandHandler(IGenericRepository<Domain.Entities.Article> articleRepository, IUnitOfWork unitOfWork)
    {
        _articleRepository = articleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateArticleResponse> Handle(UpdateArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _articleRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.Article(request.Id);

        article.Title = request.Title;
        article.Content = request.Content;
        article.Summary = request.Summary;
        article.ImageUrl = request.ImageUrl;
        article.CategoryId = request.CategoryId;
        article.UpdatedAt = DateTime.UtcNow;

        await _articleRepository.UpdateAsync(article, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateArticleResponse
        {
            Id = article.Id,
            Title = article.Title,
            Summary = article.Summary,
            CategoryId = article.CategoryId,
            UpdatedAt = article.UpdatedAt
        };
    }
}
