using FluentValidation;

namespace NewsService.Application.Validators;

public static class ArticleImageRules
{
    /// <summary>
    /// Görsel alanı iki farklı değer taşıyabilir: depo anahtarı (<c>articles/2026/08/…</c>)
    /// veya dışarıdan yapıştırılmış mutlak adres. İkisi de kabul edilir, ama
    /// <c>javascript:</c> ve <c>data:</c> gibi şemalar reddedilir — bu değer
    /// doğrudan <c>img src</c> olarak render ediliyor.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> ValidImageReference<T>(this IRuleBuilder<T, string?> rule) =>
        rule.Must(BeKeyOrHttpUrl)
            .WithMessage("Görsel adresi yalnızca http(s) ile başlayabilir veya yüklenen bir dosyanın anahtarı olmalıdır.")
            .MaximumLength(1000);

    private static bool BeKeyOrHttpUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var trimmed = value.Trim();

        // Şema içeriyorsa yalnızca http/https serbest.
        if (trimmed.Contains("://", StringComparison.Ordinal))
        {
            return (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                    || trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                && Uri.TryCreate(trimmed, UriKind.Absolute, out _);
        }

        // Şemasız değerler depo anahtarı sayılır: "javascript:alert(1)" gibi
        // iki nokta içeren ifadeler ve dizin dışına çıkma denemeleri elenir.
        return !trimmed.Contains(':', StringComparison.Ordinal)
            && !trimmed.Contains("..", StringComparison.Ordinal)
            && !trimmed.StartsWith('/');
    }
}
