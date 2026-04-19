using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Features.Commands.User.Response;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using IdentityService.Domain.Constants;
using IdentityService.Domain.Entities;
using MediatR;
using Shared.Messaging;
using Shared.Messaging.Events;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

public class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, CreateUserResponse>
{
    private readonly IKeycloakAdminClient _keycloakAdminClient;
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEventPublisher _eventPublisher;

    public RegisterUserCommandHandler(
        IKeycloakAdminClient keycloakAdminClient,
        IGenericRepository<Domain.Entities.User> userRepository,
        IUserRoleRepository userRoleRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher)
    {
        _keycloakAdminClient = keycloakAdminClient;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
    }

    public async Task<CreateUserResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Keycloak'ta user oluştur
        var keycloakId = await _keycloakAdminClient.CreateUserAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            cancellationToken);

        // 2. Keycloak'ta default "user" rolü ata
        await _keycloakAdminClient.AssignRoleAsync(keycloakId, RoleConstants.User, cancellationToken);

        try
        {
            var user = new Domain.Entities.User
            {
                KeycloakId = keycloakId,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            // 3. User'ı context'e ekle
            await _userRepository.CreateAsync(user, cancellationToken);

            // 4. DB'ye UserRole ekle — ID seed'den sabit gelir, DB sorgusu yok
            await _userRoleRepository.CreateAsync(
                new UserRole { UserId = user.Id, RoleId = RoleConstants.UserId },
                cancellationToken);

            // 5. Outbox mesajını context'e ekle
            await _eventPublisher.PublishAsync(Topics.User.Registered, new UserRegisteredEvent
            {
                UserId = user.Id,
                KeycloakId = user.KeycloakId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                RegisteredAt = user.CreatedAt
            }, cancellationToken);

            // 6. User + UserRole + OutboxMessage tek transaction'da kaydedilir
            await _unitOfWork.SaveChangesAsync(cancellationToken);

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
        catch
        {
            // DB kaydı başarısız → Keycloak'taki user'ı sil (compensating transaction)
            await _keycloakAdminClient.DeleteUserAsync(keycloakId, cancellationToken);
            throw;
        }
    }
}
