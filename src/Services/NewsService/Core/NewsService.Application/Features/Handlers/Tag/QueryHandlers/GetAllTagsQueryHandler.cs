using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsService.Application.Features.Queries.Tag.Request;
using NewsService.Application.Features.Queries.Tag.Response;
using NewsService.Application.Interfaces;

namespace NewsService.Application.Features.Handlers.Tag.QueryHandlers;

public class GetAllTagsQueryHandler : IRequestHandler<GetAllTagsQuery, List<GetAllTagsResponse>>
{
    private readonly IGenericRepository<Domain.Entities.Tag> _tagRepository;

    public GetAllTagsQueryHandler(IGenericRepository<Domain.Entities.Tag> tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<List<GetAllTagsResponse>> Handle(GetAllTagsQuery request, CancellationToken cancellationToken)
    {
        return await _tagRepository.GetQueryable()
            .Where(t => !t.IsDeleted)
            .Select(t => new GetAllTagsResponse
            {
                Id = t.Id,
                Name = t.Name,
                ArticleCount = t.ArticleTags.Count
            })
            .ToListAsync(cancellationToken);
    }
}
