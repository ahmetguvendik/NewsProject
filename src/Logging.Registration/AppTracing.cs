using System.Diagnostics;

namespace Logging.Registration;

/// <summary>
/// Elle açılan trace bölümlerinin ortak kaynağı.
///
/// NEDEN VAR: ASP.NET Core, EF Core ve HttpClient kendi bölümlerini otomatik
/// üretiyor — APM ajanı onları zaten görüyor. Görmediği yer arka plandaki iş:
/// outbox satırının Kafka'ya yazılması, inbox satırının işlenmesi. Orada isteği
/// başlatan HTTP çağrısı çoktan bitmiş oluyor.
///
/// NEDEN ActivitySource, düz `new Activity()` DEĞİL: `new Activity()` hiçbir
/// kaynağa bağlı değil ve dinleyiciler yalnızca bir ActivitySource üzerinden
/// haberdar oluyor. Düz Activity ile üretilenler loglarda trace.id taşıyordu
/// ama APM arayüzünde hiç görünmüyordu — trace, servis sınırında kopuyordu.
/// </summary>
public static class AppTracing
{
    /// <summary>
    /// Tüm process'lerde AYNI isim. APM ajanı hangi kaynakları dinleyeceğini
    /// isimden seçiyor; servis başına ayrı isim verilseydi her yeni servis
    /// ajanın yapılandırmasına elle eklenmek zorunda kalırdı.
    /// </summary>
    public const string SourceName = "NewsProject";

    public static readonly ActivitySource Source = new(SourceName);

    /// <summary>
    /// Verilen traceparent'ın altına bağlı, başlatılmış bir bölüm döndürür.
    /// Çağıran dispose etmekle yükümlü.
    /// </summary>
    /// <param name="traceParent">
    /// Olayı üreten isteğin W3C trace bağlamı (outbox/inbox satırında saklanıyor).
    /// Boşsa mevcut bağlam kullanılır.
    /// </param>
    public static Activity StartLinked(
        string name,
        string? traceParent,
        ActivityKind kind = ActivityKind.Internal)
    {
        var activity = Source.StartActivity(name, kind, parentId: traceParent!);

        if (activity is not null)
            return activity;

        // ActivitySource, dinleyici yoksa null döner — APM kapalıyken durum bu.
        // O hâlde loglardaki trace.id de kaybolurdu; bağıntı APM'den bağımsız
        // olarak çalışmaya devam etmeli, bu yüzden düz Activity'ye düşülüyor.
        var fallback = new Activity(name);

        if (!string.IsNullOrEmpty(traceParent))
            fallback.SetParentId(traceParent);

        return fallback.Start();
    }
}
