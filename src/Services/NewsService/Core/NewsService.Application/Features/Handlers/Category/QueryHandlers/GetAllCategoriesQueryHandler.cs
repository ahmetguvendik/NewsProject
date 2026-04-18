using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsService.Application.Features.Queries.Category.Request;
using NewsService.Application.Features.Queries.Category.Response;
using NewsService.Application.Interfaces;

namespace NewsService.Application.Features.Handlers.Category.QueryHandlers;

public class GetAllCategoriesQueryHandler : IRequestHandler<GetAllCategoriesQuery, List<GetAllCategoriesResponse>>
{
    private readonly IGenericRepository<Domain.Entities.Category> _categoryRepository;

    public GetAllCategoriesQueryHandler(IGenericRepository<Domain.Entities.Category> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<List<GetAllCategoriesResponse>> Handle(GetAllCategoriesQuery request, CancellationToken cancellationToken)
    {
        return await _categoryRepository.GetQueryable()
            .Where(c => !c.IsDeleted)
            .Select(c => new GetAllCategoriesResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ArticleCount = c.Articles.Count(a => !a.IsDeleted)
            })
            .ToListAsync(cancellationToken);
    }
}
