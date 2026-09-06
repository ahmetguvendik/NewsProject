namespace Shared.HealthChecks;

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

    /// <summary>Kesintisi tolere edilen bağımlılıklar; raporlanır, readiness'ı düşürmez.</summary>
    public const string Dependency = "dependency";
}
