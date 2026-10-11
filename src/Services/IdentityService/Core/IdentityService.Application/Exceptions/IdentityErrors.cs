using Shared.Exceptions;

namespace IdentityService.Application.Exceptions;

public static class IdentityErrors
{
    extension(NotFoundException)
    {
        public static NotFoundException User(Guid id) => new(
            ErrorCodes.User.NotFound,
            "Kullanıcı bulunamadı.",
            "Kullanıcı silinmiş olabilir. Listeyi yenileyip tekrar deneyin.");

        public static NotFoundException UserRole(Guid userId, string roleName) => new(
            ErrorCodes.User.RoleNotFound,
            "Kullanıcı rolü bulunamadı.",
            $"Kullanıcının '{roleName}' rolü yok. Listeyi yenileyip tekrar deneyin.");
    }

    extension(ConflictException)
    {
        public static ConflictException UserAlreadyExists(string email) => new(
            ErrorCodes.User.AlreadyExists,
            "Bu e-posta adresi zaten kayıtlı.",
            $"'{email}' adresiyle bir kullanıcı zaten mevcut. Farklı bir e-posta deneyin veya giriş yapın.");

        public static ConflictException RoleAlreadyAssigned(Guid userId, string roleName) => new(
            ErrorCodes.User.RoleAlreadyAssigned,
            "Rol zaten atanmış.",
            $"Kullanıcıda '{roleName}' rolü zaten var.");
    }

    extension(BusinessException)
    {
        public static BusinessException LastAdminRoleRemoval() => new(
            ErrorCodes.User.LastAdminCannotBeRemoved,
            "Son aktif adminin rolü kaldırılamaz.",
            "Sistemde en az bir aktif admin kalmalı. Önce başka bir kullanıcıya admin rolü atayın.");

        public static BusinessException LastAdminDeactivation() => new(
            ErrorCodes.User.LastAdminCannotBeDeactivated,
            "Son aktif admin pasife alınamaz.",
            "Sistemde en az bir aktif admin kalmalı. Önce başka bir kullanıcıya admin rolü atayın.");

        public static BusinessException SelfDeactivation() => new(
            ErrorCodes.User.CannotDeactivateSelf,
            "Kendi hesabınızı pasife alamazsınız.",
            "Gerekirse bu işlemi başka bir admin yapabilir.");
    }

    extension(ExternalServiceException)
    {
        public static ExternalServiceException Keycloak(string errorCode, string operation, string detail) => new(
            errorCode,
            "Kimlik sağlayıcısıyla iletişim kurulamadı.",
            $"Keycloak '{operation}' işlemi başarısız oldu: {detail}");
    }
}
