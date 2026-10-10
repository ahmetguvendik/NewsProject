using System.Globalization;
using System.Net;

namespace NotificationInboxWorker.Email;

// Dışarıdan gelen her değer encode edilir: kayıt formundaki ad, maile link olarak sızmasın.
public static class EmailTemplates
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");
    private static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");

    public static (string Subject, string Body) Welcome(string firstName, string lastName) => (
        "Telgraf'a Hoşgeldiniz! 🎉",
        $"""
        <html><body style="font-family:Arial,sans-serif;padding:20px">
            <h2>Merhaba {Html(firstName)} {Html(lastName)},</h2>
            <p>Telgraf'a hoşgeldiniz! Hesabınız başarıyla oluşturuldu.</p>
            <p>Artık haberleri okuyabilir ve daha fazlasını keşfedebilirsiniz.</p>
            <br/><p>İyi okumalar,</p><p><strong>Telgraf Ekibi</strong></p>
        </body></html>
        """);

    public static (string Subject, string Body) ArticlePublished(string firstName, string title, DateTime publishedAtUtc) => (
        $"Haberiniz Yayınlandı: {title}",
        $"""
        <html><body style="font-family:Arial,sans-serif;padding:20px">
            <h2>Merhaba {Html(firstName)},</h2>
            <p>"<strong>{Html(title)}</strong>" başlıklı haberiniz yayına alındı! 📰</p>
            <p><strong>Yayın Tarihi:</strong> {Date(publishedAtUtc)}</p>
            <br/><p><strong>Telgraf Ekibi</strong></p>
        </body></html>
        """);

    public static (string Subject, string Body) ArticleBroadcast(string firstName, string title, DateTime publishedAtUtc) => (
        $"Yeni haber: {title}",
        $"""
        <html><body style="font-family:Arial,sans-serif;padding:20px">
            <h2>Merhaba {Html(firstName)},</h2>
            <p>Telgraf'ta yeni bir haber yayınlandı:</p>
            <p style="font-size:18px"><strong>{Html(title)}</strong></p>
            <p><strong>Yayın Tarihi:</strong> {Date(publishedAtUtc)}</p>
            <br/><p>İyi okumalar,</p><p><strong>Telgraf Ekibi</strong></p>
            <hr/>
            <p style="font-size:12px;color:#888">
                Bu e-postayı bülten aboneliğiniz olduğu için aldınız.
                Hesabım sayfasından aboneliğinizi kapatabilirsiniz.
            </p>
        </body></html>
        """);

    private static string Html(string value) => WebUtility.HtmlEncode(value);

    private static string Date(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Istanbul)
            .ToString("dd MMMM yyyy HH:mm", Turkish);
}
