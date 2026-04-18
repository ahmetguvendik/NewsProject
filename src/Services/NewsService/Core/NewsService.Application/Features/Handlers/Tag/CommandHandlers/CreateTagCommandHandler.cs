using MediatR;
using NewsService.Application.Features.Commands.Tag.Request;
using NewsService.Application.Features.Commands.Tag.Response;
using NewsService.Application.Interfaces;
using NewsService.Application.UnitOfWorks;

namespace NewsService.Application.Features.Handlers.Tag.CommandHandlers;

public class CreateTagCommandHandler : IRequestHandler<CreateTagCommand, CreateTagResponse>
{
    private readonly IGenericRepository<Domain.Entities.Tag> _tagRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTagCommandHandler(IGenericRepository<Domain.Entities.Tag> tagRepository, IUnitOfWork unitOfWork)
    {
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateTagResponse> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        var tag = new Domain.Entities.Tag { Name = request.Name };
        await _tagRepository.CreateAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new CreateTagResponse { Id = tag.Id, Name = tag.Name, CreatedAt = tag.CreatedAt };
    }
}
