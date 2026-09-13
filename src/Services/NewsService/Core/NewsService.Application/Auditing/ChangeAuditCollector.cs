namespace NewsService.Application.Auditing;

/// <summary>
/// Bir istek boyunca biriken veri değişikliklerini taşır.
///
/// NEDEN VAR: değişikliği BİLEN yer ile onu YAZMASI gereken yer farklı katmanlarda.
/// Eski ve yeni değeri yalnızca EF Core'un ChangeTracker'ı biliyor (Persistance),
/// komutun log satırını ise CommandLoggingBehavior yazıyor (Application). Araya bu
/// taşıyıcı konmadan önce ikisi ayrı ayrı yazıyordu ve tek bir güncelleme loglarda
/// üç satıra bölünüyordu — okuyanın satırları trace.id ile eşleştirmesi gerekiyordu.
///
/// Scoped: her HTTP isteği kendi örneğini alır, istekler birbirinin değişikliklerini
/// görmez.
/// </summary>
public interface IChangeAuditCollector
{
    /// <summary>Kaydetme sırasında tespit edilen bir değişikliği ekler.</summary>
    void Add(string change);

    /// <summary>
    /// Biriken değişiklikleri döndürür ve listeyi boşaltır.
    ///
    /// Boşaltmak önemli: tek bir istek içinde birden fazla komut çalışabilir
    /// (ya da bir komut birden çok kez kaydedebilir) ve ikinci satır birincinin
    /// değişikliklerini tekrar yazmamalı.
    /// </summary>
    IReadOnlyList<string> Drain();
}

public sealed class ChangeAuditCollector : IChangeAuditCollector
{
    private readonly List<string> _changes = [];

    public void Add(string change) => _changes.Add(change);

    public IReadOnlyList<string> Drain()
    {
        if (_changes.Count == 0)
            return [];

        var snapshot = _changes.ToArray();
        _changes.Clear();
        return snapshot;
    }
}
