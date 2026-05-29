using QuickNET.Models;

namespace QuickNET.History;

public class HistoryService
{
    private readonly HistoryManager _manager;

    public HistoryService(HistoryManager manager)
    {
        _manager = manager;
    }

    public void Record(string input, string language, string output, bool isError)
    {
        _manager.AddEntry(new HistoryEntry
        {
            Timestamp = DateTime.Now,
            Language = language,
            Input = input,
            Output = output,
            IsError = isError
        });
    }

    public IReadOnlyList<HistoryEntry> GetEntries() => _manager.Entries;

    public void Clear() => _manager.Clear();
}
