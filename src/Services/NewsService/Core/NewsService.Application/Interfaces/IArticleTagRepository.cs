using NewsService.Domain.Entities;

namespace NewsService.Application.Interfaces;

public interface IArticleTagRepository
{
    Task<List<ArticleTag>> GetByArticleIdAsync(Guid articleId, CancellationToken cancellationToken = default);
    Task CreateAsync(ArticleTag articleTag, CancellationToken cancellationToken = default);
    Task DeleteAsync(ArticleTag articleTag, CancellationToken cancellationToken = default);
    IQueryable<ArticleTag> GetQueryable();
}
