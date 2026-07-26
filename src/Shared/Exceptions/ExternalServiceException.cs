namespace Shared.Exceptions;

/// <summary>
/// Dış bir servise (Keycloak, SMTP vb.) yapılan çağrı beklenmedik şekilde başarısız
/// olduğunda fırlatılır. Hata istemcinin gönderdiği veriden değil, bağımlı servisin
/// erişilemez veya yanlış yapılandırılmış olmasından kaynaklanır. → HTTP 502
/// </summary>
public class ExternalServiceException : AppException
{
    public ExternalServiceException(
        string errorCode,
        string message,
        string description)
        : base(502, errorCode, message, description)
    {
    }

    // ── Hazır factory metotları ──────────────────────────────────────

    public static ExternalServiceException Keycloak(string errorCode, string operation, string detail) => new(
        errorCode,
        "Kimlik sağlayıcısıyla iletişim kurulamadı.",
        $"Keycloak '{operation}' işlemi başarısız oldu: {detail}");
}
