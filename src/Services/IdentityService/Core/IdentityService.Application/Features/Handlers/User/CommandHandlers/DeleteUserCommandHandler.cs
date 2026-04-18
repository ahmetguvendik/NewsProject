using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using MediatR;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserCommandHandler(IGenericRepository<Domain.Entities.User> userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.Id.ToString(), cancellationToken)
            ?? throw new Exception($"User not found: {request.Id}");

        await _userRepository.DeleteAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
