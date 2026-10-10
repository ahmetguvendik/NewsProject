using NewsService.Application.Auditing.Abstract;

namespace NewsService.Application.Auditing.Concrete;

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
