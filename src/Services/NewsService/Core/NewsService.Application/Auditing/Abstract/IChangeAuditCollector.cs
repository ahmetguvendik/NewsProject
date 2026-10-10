namespace NewsService.Application.Auditing.Abstract;

public interface IChangeAuditCollector
{
    void Add(string change);

    IReadOnlyList<string> Drain();
}
