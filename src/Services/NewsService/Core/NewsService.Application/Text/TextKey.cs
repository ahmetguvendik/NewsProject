namespace NewsService.Application.Text;

// Postgres lower() 'I' ve 'İ'yi 'i' yapar, .NET ise 'İ'yi küçültmez: karşılaştırmada iki taraf aynı sonucu versin.
public static class TextKey
{
    public static string Lower(string value) => value.Trim().Replace('İ', 'i').ToLowerInvariant();
}
