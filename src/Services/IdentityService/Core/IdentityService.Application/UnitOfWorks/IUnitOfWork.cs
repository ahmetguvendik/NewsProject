using System.Data;

namespace IdentityService.Application.UnitOfWorks;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Açık bir transaction başlatır.
    ///
    /// ÇOĞU İŞTE GEREKMEZ: tek bir <see cref="SaveChangesAsync"/> zaten kendi
    /// transaction'ında çalışıyor — o çağrıdaki bütün değişiklikler ya hep
    /// birlikte yazılıyor ya hiç. Outbox'un "varlık ve olayı birlikte" garantisi
    /// de oradan geliyor. Bunu yalnızca birden fazla okuma/yazmanın BİRLİKTE
    /// tutarlı olması gerektiğinde kullanın.
    ///
    /// İZOLASYON SEVİYESİ ZORUNLU, çünkü asıl işi o yapıyor. Tipik kullanım bir
    /// kontrol-sonra-eylem kuralı ("son admin kaldırılamaz": önce say, sonra
    /// sil). Postgres'in varsayılanı ReadCommitted bu yarışı ENGELLEMİYOR —
    /// eş zamanlı iki istek aynı eski sayıyı görüp ikisi de siliyor.
    /// Yalnızca <see cref="IsolationLevel.Serializable"/> engelliyor: çakışan
    /// iki işlemden birini iptal ediyor ve istemciye 409 dönüyor.
    /// </summary>
    Task BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default);

    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
