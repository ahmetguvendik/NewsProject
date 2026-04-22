namespace Shared.Exceptions;

/// <summary>
/// Tüm domain / uygulama hatalarının base class'ı.
/// Her hata bir HTTP status code, bir error code, insan okunabilir bir mesaj
/// ve detaylı bir açıklama taşır.
/// </summary>
public abstract class AppException : Exception
{
    /// <summary>HTTP status kodu (ör. 404, 409, 422).</summary>
    public int StatusCode { get; }

    /// <summary>Makine okunabilir sabit kod (ör. "ARTICLE_NOT_FOUND").</summary>
    public string ErrorCode { get; }

    /// <summary>Kısa, kullanıcıya gösterilebilir mesaj.</summary>
    public override string Message { get; }

    /// <summary>Geliştiriciye yönelik detaylı açıklama.</summary>
    public string Description { get; }

    protected AppException(
        int statusCode,
        string errorCode,
        string message,
        string description)
        : base(message)
    {
        StatusCode  = statusCode;
        ErrorCode   = errorCode;
        Message     = message;
        Description = description;
    }
}
