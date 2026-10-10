namespace Shared.Exceptions;

public class ExternalServiceException : AppException
{
    public ExternalServiceException(
        string errorCode,
        string message,
        string description)
        : base(502, errorCode, message, description)
    {
    }
}
