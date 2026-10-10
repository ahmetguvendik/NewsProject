namespace Shared.Exceptions;

public class ForbiddenException : AppException
{
    public ForbiddenException(
        string errorCode,
        string message,
        string description)
        : base(403, errorCode, message, description)
    {
    }
}
