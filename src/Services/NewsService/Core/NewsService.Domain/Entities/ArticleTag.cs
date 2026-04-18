namespace NewsService.Domain.Entities;

public class ArticleTag
{
    public Guid ArticleId { get; set; }
    public Guid TagId { get; set; }
    public Article Article { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}
