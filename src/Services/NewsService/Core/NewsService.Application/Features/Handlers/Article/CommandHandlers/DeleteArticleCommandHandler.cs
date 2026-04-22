using MediatR;
using NewsService.Application.Features.Commands.Article.Request;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;
using Shared.Exceptions;

namespace NewsService.Application.Features.Handlers.Article.CommandHandlers;

public class DeleteArticleCommandHandler : IRequestHandler<DeleteArticleCommand>
{
    private readonly IGenericRepository<Domain.Entities.Article> _articleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteArticleCommandHandler(IGenericRepository<Domain.Entities.Article> articleRepository, IUnitOfWork unitOfWork)
    {
        _articleRepository = articleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _articleRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.Article(request.Id);

        await _articleRepository.DeleteAsync(article, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
