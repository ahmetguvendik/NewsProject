namespace NewsService.Application.Auditing;

public interface IChangeAuditCollector
{
    void Add(string change);

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
