using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NewsService.Application.Auditing.Abstract;
using NewsService.Domain.Entities;

namespace NewsService.Persistance.Auditing;

// SavingChanges'te çalışır: kayıttan sonra eski değerler kaybolur.
internal sealed class ChangeAuditInterceptor : SaveChangesInterceptor
{
    private readonly IChangeAuditCollector _collector;

    private const int MaxValueLength = 120;

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

            if (entry.State == EntityState.Modified)
                CollectModified(entry);
        }

        if (tagEdits.Count > 0)
            CollectTagEdits(context, tagEdits);
    }

    private void CollectModified(EntityEntry entry)
    {
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

    private static string Name(Dictionary<Guid, string> names, Guid tagId) =>
        names.TryGetValue(tagId, out var name) ? name : tagId.ToString()[..8];

    private static string Format(object? value) => value switch
    {
        null => "boş",
        string s when s.Length > MaxValueLength => $"({s.Length} karakterlik metin)",
        string s => $"'{s}'",
        bool b => b ? "evet" : "hayır",
        _ => value.ToString() ?? "boş"
    };
}
