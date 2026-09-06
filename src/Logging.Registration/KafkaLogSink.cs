using Confluent.Kafka;
using Elastic.CommonSchema.Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Logging.Registration;

/// <summary>
/// Log olaylarını ECS formatında Kafka'ya yazar.
///
/// Elle yazıldı çünkü Serilog.Sinks.Kafka 2016'dan beri prerelease ve bakımsız.
/// Toplu yazma ve zamanlama <see cref="PeriodicBatchingSink"/>'ten geliyor, burada
/// yalnızca "bu partiyi Kafka'ya bas" kısmı var.
///
/// TEMEL KURAL: bu sınıf hiçbir koşulda exception fırlatmaz ve çağıranı bekletmez.
/// Loglama hayati olmayan bir yan yol; Kafka çöktüğünde uygulamanın isteği
/// yavaşlaması ya da hata vermesi, çözdüğünden büyük bir sorun yaratır. Aynı
/// gerekçeyle RedisCacheService'te devre kesici, event yayınlamada outbox var.
/// </summary>
internal sealed class KafkaLogSink : IBatchedLogEventSink, IDisposable
{
    private readonly IProducer<Null, string> _producer;
    private readonly EcsTextFormatter _formatter = new();
    private readonly string _topic;

    public KafkaLogSink(string bootstrapServers, string topic)
    {
        _topic = topic;

        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,

            // Kafka erişilemezken mesajlar sonsuza kadar birikmesin: kuyruk dolduğunda
            // Produce çağrısı hata döner, biz de o partiyi düşürürüz. Log kaybetmek,
            // belleği şişirip process'i düşürmekten iyidir.
            QueueBufferingMaxMessages = 100_000,
            MessageTimeoutMs = 10_000,

            // Log gönderimi kullanıcı isteğini bekletmemeli; teslim garantisi
            // gerekmiyor, hız gerekiyor.
            Acks = Acks.None,
            LingerMs = 50,
            CompressionType = CompressionType.Lz4
        };

        _producer = new ProducerBuilder<Null, string>(config)
            // Hatalar SelfLog'a gider; Serilog'un kendi hataları için ayrılmış kanal.
            // Buradan ILogger'a yazmak sonsuz döngü yaratırdı.
            .SetErrorHandler((_, e) => SelfLog.WriteLine("Kafka log sink hatası: {0}", e.Reason))
            .Build();
    }

    public Task EmitBatchAsync(IEnumerable<LogEvent> batch)
    {
        foreach (var logEvent in batch)
        {
            try
            {
                var payload = new StringWriter();
                _formatter.Format(logEvent, payload);

                // Produce (ProduceAsync değil): librdkafka'nın kendi kuyruğuna bırakır
                // ve hemen döner. Await etseydik her satır için ağ turu beklenirdi.
                _producer.Produce(_topic, new Message<Null, string> { Value = payload.ToString() });
            }
            catch (ProduceException<Null, string> ex)
            {
                // Kuyruk dolu (Kafka erişilemiyor) — bu olay düşürülür, akış devam eder.
                SelfLog.WriteLine("Log olayı düşürüldü: {0}", ex.Error.Reason);
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Log olayı biçimlendirilemedi: {0}", ex);
            }
        }

        return Task.CompletedTask;
    }

    public Task OnEmptyBatchAsync() => Task.CompletedTask;

    public void Dispose()
    {
        try
        {
            // Kapanışta bekleyenleri göndermeye çalış, ama süresiz bekleme.
            _producer.Flush(TimeSpan.FromSeconds(5));
        }
        catch (Exception ex)
        {
            SelfLog.WriteLine("Kafka log sink kapatılırken hata: {0}", ex);
        }
        finally
        {
            _producer.Dispose();
        }
    }
}
