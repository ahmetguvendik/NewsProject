namespace Shared.Exceptions;

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
