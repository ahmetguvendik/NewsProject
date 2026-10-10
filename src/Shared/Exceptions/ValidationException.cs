namespace Shared.Exceptions;

public class ValidationException : AppException
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base(
            400,
            ErrorCodes.General.ValidationFailed,
            "Doğrulama hatası oluştu.",
            "Gönderilen veriler doğrulama kurallarını karşılamıyor. Ayrıntılar için 'errors' alanını inceleyin.")
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
