using MediatR;
using NewsService.Application.Caching;
using NewsService.Application.Features.Commands.Category.Request;
using NewsService.Application.Features.Commands.Category.Response;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;

namespace NewsService.Application.Features.Handlers.Category.CommandHandlers;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CreateCategoryResponse>
{
    private readonly IGenericRepository<Domain.Entities.Category> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public CreateCategoryCommandHandler(IGenericRepository<Domain.Entities.Category> categoryRepository, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<CreateCategoryResponse> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = new Domain.Entities.Category
        {
            Name = request.Name,
            Description = request.Description
        };

        await _categoryRepository.CreateAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Liste önbelleği bayat kalmasın; okuyan sorgu aynı anahtarı kullanıyor.
        await _cache.RemoveAsync(CacheKeys.Categories, cancellationToken);

        return new CreateCategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            CreatedAt = category.CreatedAt
        };
    }
}
