namespace HealthCheck.Registration;

public static class HealthCheckTags
{
    /// <summary>
    /// Bu etiketi taşıyan kontroller <c>/health/ready</c> sonucunu belirler; yani
    /// başarısız olduklarında servise trafik yönlendirilmemesi gerekir.
    ///
    /// Etiket YALNIZCA servisin onsuz çalışamayacağı bağımlılıklara verilir.
    /// Redis, Kafka ve MinIO bilerek dışarıda: üçünün de kesintisi tasarım gereği
    /// tolere ediliyor (Redis'te devre kesici, Kafka'da outbox). Onları ready
    /// kapsamına almak, o dayanıklılığı elle iptal etmek olur — servis ayakta
    /// kalabilecekken rotasyondan düşer.
    /// </summary>
    public const string Ready = "ready";

    /// <summary>
    /// Servisin çalışmaya devam edebildiği ama bir şeyin bozuk olduğu bağımlılıklar.
    ///
    /// Hepsi Unhealthy raporluyor — "ne çökerse çöksün haberimiz olsun". Etiketin
    /// Ready OLMAMASI kritik: /health/ready etkilenmediği için container sağlıklı
    /// kalıyor ve ona bağlı servisler durmuyor. Redis'in kısa bir kesintisi tüm
    /// sistemi durduramaz; yalnızca panelde kırmızı görünür ve bildirim gider.
    /// </summary>
    public const string Dependency = "dependency";
}
