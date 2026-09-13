namespace Shared.Exceptions;

/// <summary>
/// Kullanıcı tanınıyor ama bu işlemi yapmaya yetkisi yok. → HTTP 403
///
/// <see cref="UnauthorizedException"/>'dan farkı, isteğin kim olduğu belli:
/// 401 "seni tanımıyorum", 403 "tanıyorum ama buna hakkın yok" demek. İstemci
/// tarafında ayrım önemli — 401 alınca oturum yenilenir, 403 alınca yenilemenin
/// bir faydası olmaz.
///
/// Rol bazlı kontroller <c>[Authorize(Roles = ...)]</c> ile zaten çerçeve
/// seviyesinde 403 üretiyor. Bu sınıf, rolün yetmediği — kaydın kendisine
/// bakmayı gerektiren — kurallar için: "bu makale senin mi", "yayına çıkmış mı".
/// </summary>
public class ForbiddenException : AppException
{
    public ForbiddenException(
        string errorCode,
        string message,
        string description)
        : base(403, errorCode, message, description)
    {
    }

    // ── Hazır factory metotları ──────────────────────────────────────

    public static ForbiddenException NotArticleAuthor() => new(
        ErrorCodes.Auth.Forbidden,
        "Bu haberi düzenleyemezsiniz.",
        "Editörler yalnızca kendi yazdıkları haberleri düzenleyebilir. " +
        "Başka bir yazarın haberinde değişiklik yapılması gerekiyorsa bir yöneticiye başvurun.");

    public static ForbiddenException ArticleAlreadyPublished() => new(
        ErrorCodes.Auth.Forbidden,
        "Yayındaki haber düzenlenemez.",
        "Haber yayına alındıktan sonra içeriğini yalnızca yöneticiler değiştirebilir. " +
        "Bir düzeltme gerekiyorsa bir yöneticiye başvurun.");
}
