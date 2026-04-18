using NewsService.Domain.Common;
namespace NewsService.Domain.Entities;

public class Tag : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ICollection<ArticleTag> ArticleTags { get; set; } = [];
}
