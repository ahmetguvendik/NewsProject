using MediatR;
using NewsService.Application.Caching;
using NewsService.Application.Features.Commands.Tag.Request;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;
using Shared.Exceptions;

namespace NewsService.Application.Features.Handlers.Tag.CommandHandlers;

public class DeleteTagCommandHandler : IRequestHandler<DeleteTagCommand>
{
    private readonly IGenericRepository<Domain.Entities.Tag> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public DeleteTagCommandHandler(IGenericRepository<Domain.Entities.Tag> tagRepository, IUnitOfWork unitOfWork, ICacheService cache)
    {
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task Handle(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await _tagRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.Tag(request.Id);

        await _tagRepository.DeleteAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Liste önbelleği bayat kalmasın; okuyan sorgu aynı anahtarı kullanıyor.
        await _cache.RemoveAsync(CacheKeys.Tags, cancellationToken);
    }
}
