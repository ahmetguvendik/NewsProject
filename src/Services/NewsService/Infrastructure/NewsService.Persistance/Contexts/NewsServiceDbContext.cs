using Microsoft.EntityFrameworkCore;
using NewsService.Domain.Entities;

namespace NewsService.Persistance.Contexts;

public class NewsServiceDbContext : DbContext
{
    public NewsServiceDbContext(DbContextOptions<NewsServiceDbContext> options) : base(options) { }

    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ArticleTag> ArticleTags => Set<ArticleTag>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArticleTag>().HasKey(at => new { at.ArticleId, at.TagId });

        modelBuilder.Entity<ArticleTag>()
            .HasOne(at => at.Article).WithMany(a => a.ArticleTags).HasForeignKey(at => at.ArticleId);

        modelBuilder.Entity<ArticleTag>()
            .HasOne(at => at.Tag).WithMany(t => t.ArticleTags).HasForeignKey(at => at.TagId);

        modelBuilder.Entity<Article>()
            .HasOne(a => a.Category).WithMany(c => c.Articles).HasForeignKey(a => a.CategoryId);

        modelBuilder.Entity<Category>()
            .HasIndex(c => c.Name).IsUnique();

        modelBuilder.Entity<Tag>()
            .HasIndex(t => t.Name).IsUnique();
    }
}
