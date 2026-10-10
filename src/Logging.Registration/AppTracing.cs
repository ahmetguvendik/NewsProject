using System.Diagnostics;

namespace Logging.Registration;

public static class AppTracing
{
    // Tüm process'lerde aynı: APM ajanı dinleyeceği kaynağı isimden seçiyor.
    public const string SourceName = "NewsProject";

    public static readonly ActivitySource Source = new(SourceName);

    public static Activity StartLinked(
        string name,
        string? traceParent,
        ActivityKind kind = ActivityKind.Internal)
    {
        var activity = Source.StartActivity(name, kind, parentId: traceParent!);

        if (activity is not null)
            return activity;

        // Dinleyici yoksa (APM kapalı) null döner; loglardaki trace.id kaybolmasın diye düz Activity.
        var fallback = new Activity(name);

        if (!string.IsNullOrEmpty(traceParent))
            fallback.SetParentId(traceParent);

        return fallback.Start();
    }
}
