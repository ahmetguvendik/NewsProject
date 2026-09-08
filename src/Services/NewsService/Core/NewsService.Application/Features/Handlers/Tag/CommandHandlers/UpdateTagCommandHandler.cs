using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsService.Application.Caching;
using NewsService.Application.Features.Commands.Tag.Request;
using NewsService.Application.Features.Commands.Tag.Response;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;
using Shared.Exceptions;

namespace NewsService.Application.Features.Handlers.Tag.CommandHandlers;

public class UpdateTagCommandHandler : IRequestHandler<UpdateTagCommand, UpdateTagResponse>
{
    private readonly IGenericRepository<Domain.Entities.Tag> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public UpdateTagCommandHandler(IGenericRepository<Domain.Entities.Tag> tagRepository, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task<UpdateTagResponse> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await _tagRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.Tag(request.Id);

        var nameTaken = await _tagRepository.GetQueryable()
            .AnyAsync(t => t.Id != request.Id && t.Name == request.Name, cancellationToken);

        if (nameTaken)
            throw ConflictException.TagAlreadyExists(request.Name);

        tag.Name = request.Name;
        tag.UpdatedAt = DateTime.UtcNow;

        await _tagRepository.UpdateAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Liste önbelleği bayat kalmasın; okuyan sorgu aynı anahtarı kullanıyor.
        await _cache.RemoveAsync(CacheKeys.Tags, cancellationToken);

        return new UpdateTagResponse { Id = tag.Id, Name = tag.Name, UpdatedAt = tag.UpdatedAt };
    }
}
