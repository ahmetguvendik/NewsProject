using MediatR;
using NewsService.Application.Caching.Abstract;
using NewsService.Application.Caching.Concrete;
using NewsService.Application.Features.Queries.Category.Response;

namespace NewsService.Application.Features.Queries.Category.Request;

public class GetAllCategoriesQuery : IRequest<List<GetAllCategoriesResponse>>, ICacheableQuery
{
    public string CacheKey => CacheKeys.Categories;

    // TTL bir güvenlik ağı: asıl tazeleme kategori değiştiğinde yapılan
    // invalidation ile olur. Bir sebeple o kaçarsa veri en fazla bir gün bayat kalır.
    public TimeSpan Duration => TimeSpan.FromHours(24);
}
