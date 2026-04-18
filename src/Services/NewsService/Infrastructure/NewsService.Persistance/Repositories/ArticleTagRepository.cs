using Microsoft.EntityFrameworkCore;
using NewsService.Application.Interfaces;
using NewsService.Domain.Entities;
using NewsService.Persistance.Contexts;

namespace NewsService.Persistance.Repositories;

public class ArticleTagRepository : IArticleTagRepository
{
    private readonly NewsServiceDbContext _context;

    public ArticleTagRepository(NewsServiceDbContext context)
    {
        _context = context;
    }

    public async Task<List<ArticleTag>> GetByArticleIdAsync(Guid articleId, CancellationToken cancellationToken = default)
        => await _context.ArticleTags.Where(at => at.ArticleId == articleId).ToListAsync(cancellationToken);

    public Task CreateAsync(ArticleTag articleTag, CancellationToken cancellationToken = default)
    {
        _context.ArticleTags.Add(articleTag);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ArticleTag articleTag, CancellationToken cancellationToken = default)
    {
        _context.ArticleTags.Remove(articleTag);
        return Task.CompletedTask;
    }

    public IQueryable<ArticleTag> GetQueryable() => _context.ArticleTags.AsQueryable();
}
