namespace Shared.Exceptions;

/// <summary>
/// İş kuralı ihlali durumlarında fırlatılır (ör. pasif kullanıcı işlem yapamaz). → HTTP 422
/// </summary>
public class BusinessException : AppException
{
    public BusinessException(
        string errorCode,
        string message,
        string description)
        : base(422, errorCode, message, description)
    {
    }
}
