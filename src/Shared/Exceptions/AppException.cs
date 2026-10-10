namespace Shared.Exceptions;

public abstract class AppException : Exception
{
    public int StatusCode { get; }

    public string ErrorCode { get; }

    public override string Message { get; }

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
