using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Features.Commands.User.Response;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using MediatR;
using Shared.Messaging;
using Shared.Messaging.Events;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, CreateUserResponse>
{
    private readonly IKeycloakAdminClient _keycloakAdminClient;
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public RegisterUserCommandHandler(
        IKeycloakAdminClient keycloakAdminClient,
        IGenericRepository<Domain.Entities.User> userRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher)
    {
        _keycloakAdminClient = keycloakAdminClient;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<CreateUserResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Keycloak'ta user oluştur, KeycloakId al
        var keycloakId = await _keycloakAdminClient.CreateUserAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            cancellationToken);

        // 2. DB'ye kaydet
        var user = new Domain.Entities.User
        {
            KeycloakId = keycloakId,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        await _userRepository.CreateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 3. Kafka event publish et
        await _eventPublisher.PublishAsync(Topics.User.Registered, new UserRegisteredEvent
        {
            UserId = user.Id,
            KeycloakId = user.KeycloakId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            RegisteredAt = user.CreatedAt
        }, cancellationToken);

        return new CreateUserResponse
        {
            Id = user.Id,
            KeycloakId = user.KeycloakId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive
        };
    }
}
