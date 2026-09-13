using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NewsService.Application.Auditing;
using NewsService.Domain.Entities;

namespace NewsService.Persistance.Auditing;

/// <summary>
/// Kaydetme sırasında NEYİN NEYE dönüştüğünü tespit eder ve toplayıcıya bırakır.
///
/// KENDİSİ LOG YAZMIYOR. Değişiklikleri <see cref="IChangeAuditCollector"/>'a
/// koyuyor, satırı CommandLoggingBehavior yazıyor. Sebebi: önce burada doğrudan
/// loglanıyordu ve tek bir güncelleme loglarda üç ayrı satıra bölünüyordu
/// ("Article güncellendi", "Haber etiketleri değişti", "UpdateArticleCommand
/// tamamlandı"). Okuyan kişi bunları trace.id ile eşleştirmek zorunda kalıyordu.
/// Artık hepsi komutun tek satırında.
///
/// NEDEN HANDLER'LARDA DEĞİL: eski ve yeni değeri bilen tek yer EF Core'un
/// ChangeTracker'ı. 19 komut handler'ının her birine elle karşılaştırma kodu
/// yazmak gerekirdi ve yirmincisi unutulurdu; interceptor kaydetme yolunun
/// tamamını kapsıyor.
///
/// SavingChanges'te çalışıyor, SavedChanges'te değil: kayıttan sonra ChangeTracker
/// durumu sıfırlanıyor ve eski değerler kayboluyor.
/// </summary>
internal sealed class ChangeAuditInterceptor : SaveChangesInterceptor
{
    private readonly IChangeAuditCollector _collector;

    /// <summary>
    /// Uzun metinlerin log'a olduğu gibi girmediği eşik. Bir haberin gövdesi
    /// kilobaytlarca olabilir; eski ve yeni hâlini birlikte yazmak her düzenlemede
    /// log'a iki kopya düşürürdü. Eşiği aşan alanlarda değer yerine uzunluk
    /// yazılıyor — "değişti mi" sorusunu cevaplıyor, çöp üretmiyor.
    /// </summary>
    private const int MaxValueLength = 120;

    /// <summary>
    /// Kendiliğinden değişen, kullanıcı eylemi hakkında bilgi taşımayan alanlar.
    /// UpdatedAt her güncellemede değişir; yazmak her satıra sabit gürültü ekler.
    /// </summary>
    private static readonly HashSet<string> IgnoredProperties = ["UpdatedAt", "CreatedAt"];

    public ChangeAuditInterceptor(IChangeAuditCollector collector)
    {
        _collector = collector;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            Collect(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            Collect(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    private void Collect(DbContext context)
    {
        // Etiket bağları ayrı toplanıyor: ArticleTag bir ara tablo ve satır başına
        // kayıt "etiket eklendi / etiket silindi" biçiminde anlamsız parçalar
        // üretirdi. Tek bir "etiketler: +spor, -ekonomi" ifadesinde birleşiyor.
        var tagEdits = new List<(Guid TagId, bool Added)>();

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.State is EntityState.Unchanged or EntityState.Detached)
                continue;

            // Outbox altyapı: Payload olayın tamamını taşıyor ve her yayında log'a
            // bir kopya daha düşerdi. Outbox worker zaten kendi satırını yazıyor.
            if (entry.Entity is OutboxMessage)
                continue;

            if (entry.Entity is ArticleTag articleTag)
            {
                if (entry.State is EntityState.Added)
                    tagEdits.Add((articleTag.TagId, true));
                else if (entry.State is EntityState.Deleted)
                    tagEdits.Add((articleTag.TagId, false));

                continue;
            }

            // Added ve Deleted BİLEREK atlanıyor: komutun adı (CreateArticleCommand,
            // DeleteTagCommand) zaten ne olduğunu söylüyor ve hedefin kimliği aynı
            // satırda target.* alanlarında duruyor. "Article oluşturuldu" eklemek
            // bilgi katmıyor, satırı uzatıyor.
            if (entry.State == EntityState.Modified)
                CollectModified(entry);
        }

        if (tagEdits.Count > 0)
            CollectTagEdits(context, tagEdits);
    }

    private void CollectModified(EntityEntry entry)
    {
        // Yumuşak silme teknik olarak bir güncelleme (IsDeleted false→true) ama
        // kullanıcının yaptığı şey silmek. Alan değişimi olarak yazılsaydı
        // "silindi" araması bu satırları hiç bulamazdı.
        var isSoftDelete = entry.Properties.Any(p =>
            p.Metadata.Name == "IsDeleted" && p.IsModified && p.CurrentValue is true);

        if (isSoftDelete)
        {
            _collector.Add("silindi");
            return;
        }

        foreach (var property in entry.Properties)
        {
            if (!property.IsModified || IgnoredProperties.Contains(property.Metadata.Name))
                continue;

            var before = property.OriginalValue;
            var after = property.CurrentValue;

            // EF, nesnenin tamamı Update ile işaretlendiğinde değeri değişmeyen
            // alanları da "modified" sayıyor. Gerçekten değişmeyeni yazmıyoruz.
            if (Equals(before, after))
                continue;

            _collector.Add($"{property.Metadata.Name}: {Format(before)} → {Format(after)}");
        }
    }

    private void CollectTagEdits(DbContext context, List<(Guid TagId, bool Added)> edits)
    {
        var names = ResolveTagNames(context, edits.Select(e => e.TagId));

        // Eklenenler önce, çıkarılanlar sonra: "+spor, -ekonomi" bir bakışta okunuyor.
        var parts = edits
            .OrderByDescending(e => e.Added)
            .Select(e => (e.Added ? "+" : "-") + Name(names, e.TagId));

        _collector.Add($"etiketler: {string.Join(", ", parts)}");
    }

    /// <summary>
    /// Etiket kimliklerini ada çevirir.
    ///
    /// Önce ChangeTracker'ın yerel önbelleğine bakılıyor: handler EKLENEN etiketleri
    /// doğrulamak için zaten yüklüyor, dolayısıyla onlar bedava geliyor. ÇIKARILAN
    /// etiketler yüklenmiyor — yalnızca ara tablo satırı siliniyor — ve onlar için
    /// tek bir toplu sorgu atılıyor. Bu olmadan çıkarılan etiket log'a
    /// "-48a822c0" diye kimlikle düşüyordu ve hangi etiket olduğu anlaşılmıyordu.
    ///
    /// AsNoTracking: amaç yalnızca okunabilir bir ad üretmek, bu nesnelerin
    /// izlenmesi süren kaydetme işlemine karışmamalı.
    /// </summary>
    private static Dictionary<Guid, string> ResolveTagNames(DbContext context, IEnumerable<Guid> tagIds)
    {
        var wanted = tagIds.Distinct().ToList();

        var names = context.Set<Tag>().Local
            .Where(t => wanted.Contains(t.Id))
            .ToDictionary(t => t.Id, t => t.Name);

        var missing = wanted.Where(id => !names.ContainsKey(id)).ToList();

        if (missing.Count > 0)
        {
            foreach (var tag in context.Set<Tag>().AsNoTracking().Where(t => missing.Contains(t.Id)))
                names[tag.Id] = tag.Name;
        }

        return names;
    }

    /// <summary>Adı bulunamayan etiket için kimliğin ilk parçasına düşülür.</summary>
    private static string Name(Dictionary<Guid, string> names, Guid tagId) =>
        names.TryGetValue(tagId, out var name) ? name : tagId.ToString()[..8];

    /// <summary>
    /// Değeri log'a yazılabilir hâle getirir. Uzun metinler değer yerine uzunlukla
    /// özetlenir; amaç neyin değiştiğini göstermek, içeriği arşivlemek değil.
    /// </summary>
    private static string Format(object? value) => value switch
    {
        null => "boş",
        string s when s.Length > MaxValueLength => $"({s.Length} karakterlik metin)",
        string s => $"'{s}'",
        bool b => b ? "evet" : "hayır",
        _ => value.ToString() ?? "boş"
    };
}
