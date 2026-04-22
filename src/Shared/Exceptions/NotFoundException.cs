namespace Shared.Exceptions;

/// <summary>
/// İstenen kaynak bulunamadığında fırlatılır. → HTTP 404
/// </summary>
public class NotFoundException : AppException
{
    public NotFoundException(
        string errorCode,
        string message,
        string description)
        : base(404, errorCode, message, description)
    {
    }

    // ── Hazır factory metotları ──────────────────────────────────────

    public static NotFoundException User(Guid id) => new(
        ErrorCodes.User.NotFound,
        "Kullanıcı bulunamadı.",
        $"'{id}' ID'sine sahip kullanıcı sistemde mevcut değil veya silinmiş olabilir.");

    public static NotFoundException Article(object id) => new(
        ErrorCodes.Article.NotFound,
        "Makale bulunamadı.",
        $"'{id}' ID'sine sahip makale sistemde mevcut değil veya silinmiş olabilir.");

    public static NotFoundException Category(object id) => new(
        ErrorCodes.Category.NotFound,
        "Kategori bulunamadı.",
        $"'{id}' ID'sine sahip kategori sistemde mevcut değil.");

    public static NotFoundException Tag(object id) => new(
        ErrorCodes.Tag.NotFound,
        "Etiket bulunamadı.",
        $"'{id}' ID'sine sahip etiket sistemde mevcut değil.");

    public static NotFoundException UserRole(Guid userId, string roleName) => new(
        ErrorCodes.User.RoleNotFound,
        "Kullanıcı rolü bulunamadı.",
        $"'{userId}' ID'li kullanıcıya atanmış '{roleName}' rolü bulunamadı.");
}
