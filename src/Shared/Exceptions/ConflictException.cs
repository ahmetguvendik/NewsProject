namespace Shared.Exceptions;

public class ConflictException : AppException
{
    public ConflictException(
        string errorCode,
        string message,
        string description)
        : base(409, errorCode, message, description)
    {
    }
}
