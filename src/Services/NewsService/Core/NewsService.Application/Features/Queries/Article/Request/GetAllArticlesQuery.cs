using MediatR;
using NewsService.Application.Features.Queries.Article.Response;

namespace NewsService.Application.Features.Queries.Article.Request;

public class GetAllArticlesQuery : IRequest<List<GetAllArticlesResponse>>
{
    /// <summary>Yalnızca editor/admin için true — taslaklar da listeye dahil edilir.</summary>
    public bool IncludeUnpublished { get; set; }
}
