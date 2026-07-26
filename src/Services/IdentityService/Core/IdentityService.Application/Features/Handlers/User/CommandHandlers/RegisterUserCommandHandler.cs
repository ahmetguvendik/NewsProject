using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Features.Commands.User.Response;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using IdentityService.Domain.Constants;
using IdentityService.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<RegisterUserCommandHandler> _logger;

    public RegisterUserCommandHandler(
        IKeycloakAdminClient keycloakAdminClient,
        IGenericRepository<Domain.Entities.User> userRepository,
        IUserRoleRepository userRoleRepository,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<RegisterUserCommandHandler> logger)
    {
        _keycloakAdminClient = keycloakAdminClient;
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<CreateUserResponse> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Keycloak'ta user oluştur.
        // Bu noktadan sonraki HER hata Keycloak'ta yetim bir kullanıcı bırakır,
        // bu yüzden geri kalan tüm adımlar telafi bloğunun içinde çalışır.
        var keycloakId = await _keycloakAdminClient.CreateUserAsync(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            cancellationToken);

        try
        {
            // 2. Keycloak'ta default "user" rolü ata
            await _keycloakAdminClient.AssignRoleAsync(keycloakId, RoleConstants.User, cancellationToken);

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
            // Rol ataması veya DB kaydı başarısız → Keycloak'taki user'ı sil (compensating transaction)
            await CompensateAsync(keycloakId);
            throw;
        }
    }

    /// <summary>
    /// Kayıt yarıda kaldığında Keycloak'ta oluşturulmuş kullanıcıyı temizler.
    /// Temizliğin kendisi başarısız olursa asıl hatayı gizlememek için yalnızca loglanır.
    /// </summary>
    private async Task CompensateAsync(string keycloakId)
    {
        try
        {
            // İstek iptal edilmiş olsa bile temizlik çalışmalı → CancellationToken.None
            await _keycloakAdminClient.DeleteUserAsync(keycloakId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Kayıt geri alınamadı: Keycloak kullanıcısı {KeycloakId} silinemedi. " +
                "Keycloak'ta yetim kullanıcı kaldı, manuel temizlik gerekiyor.",
                keycloakId);
        }
    }
}
