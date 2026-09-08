namespace Shared.Exceptions;

/// <summary>
/// Kaynak zaten mevcut olduğunda veya çakışan bir durum tespit edildiğinde fırlatılır. → HTTP 409
/// </summary>
public class ConflictException : AppException
{
    public ConflictException(
        string errorCode,
        string message,
        string description)
        : base(409, errorCode, message, description)
    {
    }

    // ── Hazır factory metotları ──────────────────────────────────────

    public static ConflictException UserAlreadyExists(string email) => new(
        ErrorCodes.User.AlreadyExists,
        "Bu e-posta adresi zaten kayıtlı.",
        $"'{email}' adresiyle bir kullanıcı zaten mevcut. Farklı bir e-posta deneyin veya giriş yapın.");

    public static ConflictException RoleAlreadyAssigned(Guid userId, string roleName) => new(
        ErrorCodes.User.RoleAlreadyAssigned,
        "Rol zaten atanmış.",
        $"'{roleName}' rolü '{userId}' ID'li kullanıcıya daha önce atanmış.");

    public static ConflictException ArticleAlreadyPublished(object id) => new(
        ErrorCodes.Article.AlreadyPublished,
        "Makale zaten yayınlanmış.",
        $"'{id}' ID'li makale daha önce yayınlanmış, tekrar yayınlanamaz.");

    public static ConflictException CategoryAlreadyExists(string name) => new(
        ErrorCodes.Category.AlreadyExists,
        "Bu adda bir kategori zaten var.",
        $"'{name}' adlı kategori mevcut. Farklı bir ad seçin.");

    public static ConflictException TagAlreadyExists(string name) => new(
        ErrorCodes.Tag.AlreadyExists,
        "Bu adda bir etiket zaten var.",
        $"'{name}' adlı etiket mevcut. Farklı bir ad seçin.");
}
