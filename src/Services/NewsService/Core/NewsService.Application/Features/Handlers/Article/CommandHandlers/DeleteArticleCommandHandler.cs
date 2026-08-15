using MediatR;
using NewsService.Application.Caching;
using NewsService.Application.Features.Commands.Article.Request;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;
using Shared.Exceptions;

namespace NewsService.Application.Features.Handlers.Article.CommandHandlers;

public class DeleteArticleCommandHandler : IRequestHandler<DeleteArticleCommand>
{
    private readonly IGenericRepository<Domain.Entities.Article> _articleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public DeleteArticleCommandHandler(IGenericRepository<Domain.Entities.Article> articleRepository, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _articleRepository = articleRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task Handle(DeleteArticleCommand request, CancellationToken cancellationToken)
    {
        var article = await _articleRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.Article(request.Id);

        await _articleRepository.DeleteAsync(article, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Listenin tüm varyantları (sayfa, kategori, rol) tek anahtarda;
        // biri değiştiğinde hepsi bayatladığı için tamamı düşürülüyor.
        await _cache.RemoveAsync(CacheKeys.ArticleLists, cancellationToken);
    }
}
