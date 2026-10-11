namespace Shared.Exceptions;

public class ValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base(
            400,
            ErrorCodes.General.ValidationFailed,
            "Bazı bilgiler hatalı.",
            "Aşağıdaki alanları düzeltip tekrar deneyin.")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }

    public ValidationException(string field, string message)
        : this(new Dictionary<string, string[]>
        {
            [field] = [message]
        })
    {
    }
}
