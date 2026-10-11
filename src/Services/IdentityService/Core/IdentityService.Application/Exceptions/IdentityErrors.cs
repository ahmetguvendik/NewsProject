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

    extension(ExternalServiceException)
    {
        public static ExternalServiceException Keycloak(string errorCode, string operation, string detail) => new(
            errorCode,
            "Kimlik sağlayıcısıyla iletişim kurulamadı.",
            $"Keycloak '{operation}' işlemi başarısız oldu: {detail}");
    }
}
