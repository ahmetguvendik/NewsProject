namespace Shared.Exceptions;

public class UnauthorizedException : AppException
{
    public UnauthorizedException(
        string errorCode,
        string message,
        string description)
        : base(401, errorCode, message, description)
    {
    }

    public static UnauthorizedException MissingClaim(string claimName) => new(
        ErrorCodes.Auth.Unauthorized,
        "Kimlik doğrulama bilgisi eksik.",
        $"Token içinde '{claimName}' claim'i bulunamadı. Geçerli bir token ile tekrar deneyin.");
}
