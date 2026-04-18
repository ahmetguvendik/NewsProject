using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsService.Application.Features.Queries.Article.Request;
using NewsService.Application.Features.Queries.Article.Response;
using NewsService.Application.Interfaces;

namespace NewsService.Application.Features.Handlers.Article.QueryHandlers;

public class GetAllArticlesQueryHandler : IRequestHandler<GetAllArticlesQuery, List<GetAllArticlesResponse>>
{
    private readonly IGenericRepository<Domain.Entities.Article> _articleRepository;

    public GetAllArticlesQueryHandler(IGenericRepository<Domain.Entities.Article> articleRepository)
    {
        _articleRepository = articleRepository;
    }

    public async Task<List<GetAllArticlesResponse>> Handle(GetAllArticlesQuery request, CancellationToken cancellationToken)
    {
        return await _articleRepository.GetQueryable()
            .Include(a => a.Category)
            .Where(a => !a.IsDeleted)
            .Select(a => new GetAllArticlesResponse
            {
                Id = a.Id,
                Title = a.Title,
                Summary = a.Summary,
                AuthorKeycloakId = a.AuthorKeycloakId,
                CategoryName = a.Category.Name,
                IsPublished = a.IsPublished,
                PublishedAt = a.PublishedAt,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }
}
