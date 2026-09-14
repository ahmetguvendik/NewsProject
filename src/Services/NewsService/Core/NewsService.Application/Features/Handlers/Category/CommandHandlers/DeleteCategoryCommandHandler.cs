using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsService.Application.Caching;
using NewsService.Application.Features.Commands.Category.Request;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;
using Shared.Exceptions;

namespace NewsService.Application.Features.Handlers.Category.CommandHandlers;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand>
{
    private readonly IGenericRepository<Domain.Entities.Category> _categoryRepository;
    private readonly IGenericRepository<Domain.Entities.Article> _articleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public DeleteCategoryCommandHandler(
        IGenericRepository<Domain.Entities.Category> categoryRepository,
        IGenericRepository<Domain.Entities.Article> articleRepository,
        IUnitOfWork unitOfWork, ICacheService cache)
    {
        _categoryRepository = categoryRepository;
        _articleRepository = articleRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.Category(request.Id);

        // Bağlı haber varsa silme ENGELLENİYOR.
        //
        // Etiketten farklı davranıyoruz çünkü kategori ZORUNLU ve bire-çok:
        // silindiğinde haberin categoryId'si artık var olmayan bir satırı
        // gösteriyor. Boş bırakılamaz, rastgele başka bir kategoriye taşımak da
        // sessizce yanlış veri üretir — yani "doğru" bir otomatik davranış yok.
        //
        // Önceden serbestti ve sonucu şuydu: haber yayında kalıyor, okuyucuya
        // artık var olmayan kategorinin adını gösteriyor, editör de kategorisini
        // değiştirmeden kaydedemiyordu (404 CATEGORY_NOT_FOUND).
        var articleCount = await _articleRepository.GetQueryable()
            .CountAsync(a => a.CategoryId == category.Id && !a.IsDeleted, cancellationToken);

        if (articleCount > 0)
            throw ConflictException.CategoryInUse(category.Name, articleCount);

        await _categoryRepository.DeleteAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Liste önbelleği bayat kalmasın; okuyan sorgu aynı anahtarı kullanıyor.
        await _cache.RemoveAsync(CacheKeys.Categories, cancellationToken);
    }
}
