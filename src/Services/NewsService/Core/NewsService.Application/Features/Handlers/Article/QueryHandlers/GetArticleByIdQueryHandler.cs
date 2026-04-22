using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsService.Application.Features.Queries.Article.Request;
using NewsService.Application.Features.Queries.Article.Response;
using NewsService.Application.Interfaces;
using Shared.Exceptions;

namespace NewsService.Application.Features.Handlers.Article.QueryHandlers;

public class GetArticleByIdQueryHandler : IRequestHandler<GetArticleByIdQuery, GetArticleByIdResponse>
{
    private readonly IGenericRepository<Domain.Entities.Article> _articleRepository;

    public GetArticleByIdQueryHandler(IGenericRepository<Domain.Entities.Article> articleRepository)
    {
        _articleRepository = articleRepository;
    }

    public async Task<GetArticleByIdResponse> Handle(GetArticleByIdQuery request, CancellationToken cancellationToken)
    {
        var article = await _articleRepository.GetQueryable()
            .Include(a => a.Category)
            .Include(a => a.ArticleTags).ThenInclude(at => at.Tag)
            .Where(a => a.Id == request.Id && !a.IsDeleted)
            .Select(a => new GetArticleByIdResponse
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                Summary = a.Summary,
                ImageUrl = a.ImageUrl,
                AuthorKeycloakId = a.AuthorKeycloakId,
                CategoryName = a.Category.Name,
                IsPublished = a.IsPublished,
                PublishedAt = a.PublishedAt,
                CreatedAt = a.CreatedAt,
                Tags = a.ArticleTags.Select(at => at.Tag.Name).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return article ?? throw NotFoundException.Article(request.Id);
    }
}
