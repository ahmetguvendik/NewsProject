using MediatR;
using NewsService.Application.Caching.Abstract;
using NewsService.Application.Caching.Concrete;
using NewsService.Application.Features.Queries.Tag.Response;

namespace NewsService.Application.Features.Queries.Tag.Request;

public class GetAllTagsQuery : IRequest<List<GetAllTagsResponse>>, ICacheableQuery
{
    public string CacheKey => CacheKeys.Tags;

    public TimeSpan Duration => TimeSpan.FromHours(24);
}
