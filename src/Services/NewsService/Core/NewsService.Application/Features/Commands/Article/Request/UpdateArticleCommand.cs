using MediatR;
using NewsService.Application.Features.Commands.Article.Response;

namespace NewsService.Application.Features.Commands.Article.Request;

public class UpdateArticleCommand : IRequest<UpdateArticleResponse>
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Makalenin nihai etiket kümesi. Gönderilmeyen mevcut etiketler kaldırılır,
    /// yeni gelenler eklenir. Boş liste tüm etiketleri temizler.
    /// </summary>
    public List<Guid> TagIds { get; set; } = [];
}
