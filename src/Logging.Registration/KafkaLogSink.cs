using Confluent.Kafka;
using Elastic.CommonSchema.Serilog;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;

namespace Logging.Registration;

// Asla exception fırlatmaz ve çağıranı bekletmez: Kafka çökünce istekler etkilenmemeli.
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
            // Kafka erişilemezken bellek şişmesin: kuyruk dolunca Produce hata verir, olay düşürülür.
            QueueBufferingMaxMessages = 100_000,
            MessageTimeoutMs = 10_000,
            Acks = Acks.None,
            LingerMs = 50,
            CompressionType = CompressionType.Lz4
        };

        _producer = new ProducerBuilder<Null, string>(config)
            // SelfLog'a: ILogger'a yazmak sonsuz döngü yaratırdı.
            .SetErrorHandler((_, e) => SelfLog.WriteLine("Kafka log sink hatası: {0}", e.Reason))
            .Build();
    }

    public Task EmitBatchAsync(IReadOnlyCollection<LogEvent> batch)
    {
        foreach (var logEvent in batch)
        {
            try
            {
                var payload = new StringWriter();
                _formatter.Format(logEvent, payload);

                _producer.Produce(_topic, new Message<Null, string> { Value = payload.ToString() });
            }
            catch (ProduceException<Null, string> ex)
            {
                SelfLog.WriteLine("Log olayı düşürüldü: {0}", ex.Error.Reason);
            }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Log olayı biçimlendirilemedi: {0}", ex);
            }
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        try
        {
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
