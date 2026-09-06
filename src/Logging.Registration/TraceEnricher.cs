using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Logging.Registration;

/// <summary>
/// Her log satırına içinde bulunduğu Activity'nin trace ve span kimliğini ekler.
///
/// Kendi GUID'imizi üretmek yerine .NET'in zaten oluşturduğu W3C trace context
/// kullanılıyor. Sebebi ileriye dönük: OpenTelemetry ve Elastic APM eklendiğinde
/// APM'in ürettiği trace kimlikleri ile buradaki log kayıtları AYNI kimliği
/// paylaşır, yani Kibana'da "bu trace'in loglarını göster" doğrudan çalışır.
/// Uydurma bir korelasyon kimliği kullansaydık o gün her şeyi değiştirmek gerekirdi.
///
/// ASP.NET Core her HTTP isteği için Activity'yi kendisi başlatıyor ve traceparent
/// başlığıyla servisler arasında otomatik taşıyor. Kafka bu bağlamı taşımadığı için
/// mesajlaşma yolunda elle aktarılıyor (bkz. outbox/inbox'taki TraceParent alanı).
/// </summary>
public sealed class TraceEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity is null)
            return;

        // Alan adları ECS şemasıyla birebir: Elastic APM aynı adları kullanıyor.
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("trace.id", activity.TraceId.ToString()));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("span.id", activity.SpanId.ToString()));
    }
}
