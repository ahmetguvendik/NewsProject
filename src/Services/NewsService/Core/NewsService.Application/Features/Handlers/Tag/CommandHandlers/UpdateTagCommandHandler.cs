using MediatR;
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

    public UpdateTagCommandHandler(IGenericRepository<Domain.Entities.Tag> tagRepository, IUnitOfWork unitOfWork)
    {
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UpdateTagResponse> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await _tagRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.Tag(request.Id);

        tag.Name = request.Name;
        tag.UpdatedAt = DateTime.UtcNow;

        await _tagRepository.UpdateAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateTagResponse { Id = tag.Id, Name = tag.Name, UpdatedAt = tag.UpdatedAt };
    }
}
