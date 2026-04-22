using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsService.Application.Features.Queries.Category.Request;
using NewsService.Application.Features.Queries.Category.Response;
using NewsService.Application.Interfaces;
using Shared.Exceptions;

namespace NewsService.Application.Features.Handlers.Category.QueryHandlers;

public class GetCategoryByIdQueryHandler : IRequestHandler<GetCategoryByIdQuery, GetCategoryByIdResponse>
{
    private readonly IGenericRepository<Domain.Entities.Category> _categoryRepository;

    public GetCategoryByIdQueryHandler(IGenericRepository<Domain.Entities.Category> categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    public async Task<GetCategoryByIdResponse> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetQueryable()
            .Where(c => c.Id == request.Id && !c.IsDeleted)
            .Select(c => new GetCategoryByIdResponse
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CreatedAt = c.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        return category ?? throw NotFoundException.Category(request.Id);
    }
}
