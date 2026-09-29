using System.Data;
using IdentityService.Application.Features.Commands.User.Request;
using IdentityService.Application.Interfaces;
using IdentityService.Application.UnitOfWorks;
using IdentityService.Domain.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Shared.Exceptions;
using Shared.Security;

namespace IdentityService.Application.Features.Handlers.User.CommandHandlers;

public class RemoveRoleCommandHandler : IRequestHandler<RemoveRoleCommand>
{
    private readonly IGenericRepository<Domain.Entities.User> _userRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IKeycloakAdminClient _keycloakAdminClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;

    public RemoveRoleCommandHandler(
        IGenericRepository<Domain.Entities.User> userRepository,
        IUserRoleRepository userRoleRepository,
        IKeycloakAdminClient keycloakAdminClient,
        IUnitOfWork unitOfWork,
        ICacheService cache)
    {
        _userRepository = userRepository;
        _userRoleRepository = userRoleRepository;
        _keycloakAdminClient = keycloakAdminClient;
        _unitOfWork = unitOfWork;
        _cache = cache;
    }

    public async Task Handle(RemoveRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId.ToString(), cancellationToken)
            ?? throw NotFoundException.User(request.UserId);

        // Bilinmeyen rol adı → 400 ValidationException
        var roleId = RoleConstants.TryGetId(request.RoleName)
            ?? throw new ValidationException("roleName",
                $"'{request.RoleName}' geçerli bir rol değil. Geçerli roller: admin, editor, user.");

        // SERIALIZABLE TRANSACTION — "son admin kaldırılamaz" kuralı yüzünden.
        //
        // Kural bir kontrol-sonra-eylem: önce admin sayısı okunuyor, sonra rol
        // siliniyor. Transaction'sız (ya da Postgres'in varsayılanı
        // ReadCommitted ile) iki admin AYNI ANDA birbirinin rolünü kaldırırsa
        // ikisi de "2 admin var" görür, ikisi de siler — sistemde hiç admin
        // kalmaz ve yönetim paneline kimse giremez.
        //
        // Serializable'da Postgres iki işlemin birbirinin okuduğu veriyi
        // değiştirdiğini fark edip birini iptal ediyor. Kaybeden 409 "işlem
        // çakıştı" alıyor; tekrar denediğinde sayı artık 1 ve kural onu
        // durduruyor. Sayma ve silme bu yüzden AYNI transaction'ın içinde.
        //
        // Transaction yalnızca admin rolü için değil her rol kaldırmada açılıyor:
        // iki ayrı kod yolu tutmaya değmeyecek kadar seyrek bir işlem.
        await _unitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var removedInKeycloak = false;
        try
        {
            // Kullanıcıda bu rol zaten yoksa kaldıracak bir şey yok → 404
            var existing = await _userRoleRepository.GetAsync(user.Id, roleId, cancellationToken)
                ?? throw NotFoundException.UserRole(request.UserId, request.RoleName);

            if (roleId == RoleConstants.AdminId)
            {
                var adminCount = await _userRoleRepository.GetQueryable()
                    .CountAsync(ur => ur.RoleId == RoleConstants.AdminId, cancellationToken);

                if (adminCount <= 1)
                    throw new BusinessException(
                        ErrorCodes.User.LastAdminCannotBeRemoved,
                        "Son admin rolü kaldırılamaz.",
                        "Sistemde en az bir admin kalmalı. Önce başka bir kullanıcıya admin rolü atayın, sonra bunu kaldırın.");
            }

            // Keycloak veritabanının dışında, transaction onu kapsamıyor. DB
            // tarafı iptal olursa aşağıdaki catch Keycloak'taki kaldırmayı geri
            // alıyor (telafi eden işlem).
            await _keycloakAdminClient.RemoveRoleAsync(user.KeycloakId, request.RoleName, cancellationToken);
            removedInKeycloak = true;

            await _userRoleRepository.DeleteAsync(existing, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Serializable çakışması çoğunlukla burada çıkıyor: yarışı kaybeden
            // işlem commit anında iptal ediliyor.
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            // İstek iptal edilmiş olsa bile geri alma ve telafi çalışmalı.
            await _unitOfWork.RollbackTransactionAsync(CancellationToken.None);

            // Keycloak'a henüz dokunulmadıysa (404, son admin kuralı, ya da
            // Keycloak çağrısının kendisi başarısız) geri alınacak bir şey yok.
            if (removedInKeycloak)
                await _keycloakAdminClient.AssignRoleAsync(user.KeycloakId, request.RoleName, CancellationToken.None);

            throw;
        }

        // Roller token'ın İÇİNDE. Keycloak'tan ve DB'den kaldırmak,
        // kullanıcının elindeki token'ı etkilemiyor: süresi dolana kadar
        // (1 saat) eski rolüyle çalışmaya devam ediyordu. Ölçüldüğünde
        // admin rolü kaldırılan kullanıcı, o token'la POST /api/user/roles
        // çağırıp kendine admin'i geri verebiliyordu.
        //
        // COMMIT'TEN SONRA: transaction iptal olursa rol aslında kalkmamış
        // demektir, damga yazılsaydı kullanıcı boşuna oturumdan atılırdı.
        await _cache.SetAsync(
            TokenRevocation.Key(user.KeycloakId),
            TokenRevocation.Now(TokenRevocation.ReasonRoleChange),
            TokenRevocation.Retention,
            cancellationToken);
    }
}
