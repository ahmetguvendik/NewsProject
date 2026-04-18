using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsService.Application.Features.Queries.Tag.Request;
using NewsService.Application.Features.Queries.Tag.Response;
using NewsService.Application.Interfaces;

namespace NewsService.Application.Features.Handlers.Tag.QueryHandlers;

public class GetTagByIdQueryHandler : IRequestHandler<GetTagByIdQuery, GetTagByIdResponse?>
{
    private readonly IGenericRepository<Domain.Entities.Tag> _tagRepository;

    public GetTagByIdQueryHandler(IGenericRepository<Domain.Entities.Tag> tagRepository)
    {
        _tagRepository = tagRepository;
    }

    public async Task<GetTagByIdResponse?> Handle(GetTagByIdQuery request, CancellationToken cancellationToken)
    {
        return await _tagRepository.GetQueryable()
            .Where(t => t.Id == request.Id && !t.IsDeleted)
            .Select(t => new GetTagByIdResponse
            {
                Id = t.Id,
                Name = t.Name,
                CreatedAt = t.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
