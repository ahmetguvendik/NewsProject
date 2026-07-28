namespace NotificationService.Domain.Entities;

public class InboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string MessageId { get; set; } = string.Empty;  // duplicate kontrolü için Kafka key
    public string Topic { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public bool IsProcessed { get; set; }
    public string? Error { get; set; }

    /// <summary>Başarısız işleme denemesi sayısı.</summary>
    public int RetryCount { get; set; }

    /// <summary>RetryCount, worker'ın MaxRetryCount eşiğini aşınca true olur; bir daha denenmez.</summary>
    public bool IsDeadLettered { get; set; }
}
