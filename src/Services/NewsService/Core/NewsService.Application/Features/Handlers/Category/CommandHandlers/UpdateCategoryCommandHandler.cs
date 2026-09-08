using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsService.Application.Caching;
using NewsService.Application.Features.Commands.Category.Request;
using NewsService.Application.Features.Commands.Category.Response;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;
using Shared.Exceptions;

namespace NewsService.Application.Features.Handlers.Category.CommandHandlers;

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, UpdateCategoryResponse>
{
    private readonly IGenericRepository<Domain.Entities.Category> _categoryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public UpdateCategoryCommandHandler(IGenericRepository<Domain.Entities.Category> categoryRepository, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _categoryRepository = categoryRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<UpdateCategoryResponse> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.Category(request.Id);

        // Başka bir kategori bu adı kullanıyorsa çakışma; kendi adını korumak serbest.
        var nameTaken = await _categoryRepository.GetQueryable()
            .AnyAsync(c => c.Id != request.Id && c.Name == request.Name, cancellationToken);

        if (nameTaken)
            throw ConflictException.CategoryAlreadyExists(request.Name);

        category.Name = request.Name;
        category.Description = request.Description;
        category.UpdatedAt = DateTime.UtcNow;

        await _categoryRepository.UpdateAsync(category, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Liste önbelleği bayat kalmasın; okuyan sorgu aynı anahtarı kullanıyor.
        await _cache.RemoveAsync(CacheKeys.Categories, cancellationToken);

        return new UpdateCategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            UpdatedAt = category.UpdatedAt
        };
    }
}
