namespace Shared.Models;

/// <summary>
/// Tüm servislerden dönen standart hata yanıtı.
/// </summary>
public sealed record ErrorResponse
{
    /// <summary>HTTP status kodu (ör. 404).</summary>
    public int Status { get; init; }

    /// <summary>Makine okunabilir sabit kod (ör. "ARTICLE_NOT_FOUND").</summary>
    public string ErrorCode { get; init; } = default!;

    /// <summary>Kullanıcıya gösterilebilecek kısa mesaj.</summary>
    public string Message { get; init; } = default!;

    /// <summary>Geliştiriciye yönelik detaylı açıklama.</summary>
    public string Description { get; init; } = default!;

    /// <summary>
    /// Doğrulama hatalarında dolan alan → hatalar sözlüğü.
    /// Diğer hata türlerinde null döner.
    /// </summary>
    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    /// <summary>Hatanın oluştuğu UTC zaman damgası.</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
