namespace Shared.Exceptions;

public class NotFoundException : AppException
{
    public NotFoundException(
        string errorCode,
        string message,
        string description)
        : base(404, errorCode, message, description)
    {
    }
}
