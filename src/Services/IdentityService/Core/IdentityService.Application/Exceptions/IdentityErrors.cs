using Shared.Exceptions;

namespace IdentityService.Application.Exceptions;

public static class IdentityErrors
{
    extension(NotFoundException)
    {
        public static NotFoundException User(Guid id) => new(
            ErrorCodes.User.NotFound,
            "Kullanıcı bulunamadı.",
            $"'{id}' ID'sine sahip kullanıcı sistemde mevcut değil veya silinmiş olabilir.");

        public static NotFoundException UserRole(Guid userId, string roleName) => new(
            ErrorCodes.User.RoleNotFound,
            "Kullanıcı rolü bulunamadı.",
            $"'{userId}' ID'li kullanıcıya atanmış '{roleName}' rolü bulunamadı.");
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
            $"'{roleName}' rolü '{userId}' ID'li kullanıcıya daha önce atanmış.");
    }

    extension(ExternalServiceException)
    {
        public static ExternalServiceException Keycloak(string errorCode, string operation, string detail) => new(
            errorCode,
            "Kimlik sağlayıcısıyla iletişim kurulamadı.",
            $"Keycloak '{operation}' işlemi başarısız oldu: {detail}");

        public static ExternalServiceException TokenRevocationStore() => new(
            ErrorCodes.Auth.RevocationUnavailable,
            "İşlem güvenlik nedeniyle tamamlanamadı.",
            "Oturum iptal kaydı yazılamadı. Biraz sonra tekrar deneyin.");
    }
}
