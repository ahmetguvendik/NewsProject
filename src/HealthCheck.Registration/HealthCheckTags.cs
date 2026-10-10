namespace HealthCheck.Registration;

public static class HealthCheckTags
{
    // Yalnızca servisin onsuz çalışamayacağı bağımlılıklar; Redis/Kafka/MinIO kesintisi bilerek tolere ediliyor.
    public const string Ready = "ready";

    // Paneli kırmızıya çevirir ama /health/ready'yi etkilemez: container durdurulmaz.
    public const string Dependency = "dependency";
}
