namespace Shared.Exceptions;

/// <summary>
/// Kimlik doğrulaması başarısız olduğunda veya token geçersiz/eksik olduğunda fırlatılır. → HTTP 401
/// </summary>
public class UnauthorizedException : AppException
{
    public UnauthorizedException(
        string errorCode,
        string message,
        string description)
        : base(401, errorCode, message, description)
    {
    }

    // ── Hazır factory metotları ──────────────────────────────────────

    public static UnauthorizedException MissingClaim(string claimName) => new(
        ErrorCodes.Auth.Unauthorized,
        "Kimlik doğrulama bilgisi eksik.",
        $"Token içinde '{claimName}' claim'i bulunamadı. Geçerli bir token ile tekrar deneyin.");

    public static UnauthorizedException InvalidToken() => new(
        ErrorCodes.Auth.TokenInvalid,
        "Geçersiz token.",
        "Sağlanan token doğrulanamadı veya süresi dolmuş.");
}
