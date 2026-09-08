using MediatR;
using Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using NewsService.Application.Caching;
using NewsService.Application.Features.Commands.Tag.Request;
using NewsService.Application.Features.Commands.Tag.Response;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;

namespace NewsService.Application.Features.Handlers.Tag.CommandHandlers;

public class CreateTagCommandHandler : IRequestHandler<CreateTagCommand, CreateTagResponse>
{
    private readonly IGenericRepository<Domain.Entities.Tag> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public CreateTagCommandHandler(IGenericRepository<Domain.Entities.Tag> tagRepository, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<CreateTagResponse> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        var exists = await _tagRepository.GetQueryable()
            .AnyAsync(t => t.Name == request.Name, cancellationToken);

        if (exists)
            throw ConflictException.TagAlreadyExists(request.Name);

        var tag = new Domain.Entities.Tag { Name = request.Name };
        await _tagRepository.CreateAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Liste önbelleği bayat kalmasın; okuyan sorgu aynı anahtarı kullanıyor.
        await _cache.RemoveAsync(CacheKeys.Tags, cancellationToken);
        return new CreateTagResponse { Id = tag.Id, Name = tag.Name, CreatedAt = tag.CreatedAt };
    }
}
