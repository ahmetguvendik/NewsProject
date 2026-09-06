namespace IdentityService.Domain.Entities;

public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Topic { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public bool IsProcessed { get; set; }
    public string? Error { get; set; }

    /// <summary>Başarısız yayınlama denemesi sayısı.</summary>
    public int RetryCount { get; set; }

    /// <summary>RetryCount, worker'ın MaxRetryCount eşiğini aşınca true olur; bir daha denenmez.</summary>
    public bool IsDeadLettered { get; set; }

    /// <summary>
    /// Olayı üreten isteğin W3C traceparent değeri ("00-{trace}-{span}-{flags}").
    ///
    /// Worker ayrı bir process'te ve saniyeler sonra çalıştığı için isteğin Activity'si
    /// çoktan ölmüş oluyor; bağlamı taşımanın tek yolu satırın kendisi. Worker bunu
    /// okuyup Activity'yi yeniden kurunca, logları isteği başlatan trace ile aynı
    /// kimliği taşıyor.
    ///
    /// Nullable: bu alandan önce yazılmış satırlar ve HTTP dışı kaynaklar için boş kalır.
    /// </summary>
    public string? TraceParent { get; set; }

}
