using System.Text.Json;

namespace QuickNET.History;

public class InputHistoryService
{
    private const int MaxEntries = 50;
    private const string HistoryFileName = "input-history.json";

    private readonly List<string> _history = new(MaxEntries + 1);
    private int _navigationIndex = -1;
    private string? _draft;
    private readonly string _filePath;

    public InputHistoryService()
    {
        _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuickNET",
            HistoryFileName);
        Load();
    }

    internal InputHistoryService(string filePath)
    {
        _filePath = filePath;
        var dir = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(dir);
        Load();
    }

    public void Record(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        if (_history.Count > 0 && _history[^1] == input)
            return;

        _history.Add(input);

        while (_history.Count > MaxEntries)
            _history.RemoveAt(0);

        Reset();
        Save();
    }

    public string? NavigateOlder(string currentDraft)
    {
        if (_history.Count == 0) return null;

        if (_navigationIndex == -1)
            _draft = currentDraft;

        if (_navigationIndex < _history.Count - 1)
            _navigationIndex++;

        return _history[^(1 + _navigationIndex)];
    }

    public string? NavigateNewer()
    {
        if (_navigationIndex <= -1) return null;

        if (_navigationIndex > 0)
        {
            _navigationIndex--;
            return _history[^(1 + _navigationIndex)];
        }
        else
        {
            _navigationIndex = -1;
            var draft = _draft;
            _draft = null;
            return draft ?? "";
        }
    }

    public void Reset()
    {
        _navigationIndex = -1;
        _draft = null;
    }

    public IReadOnlyList<string> GetEntries() => _history.AsReadOnly();

    public int Count => _history.Count;

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var entries = JsonSerializer.Deserialize<List<string>>(json);
                if (entries is not null)
                {
                    _history.AddRange(entries);
                    while (_history.Count > MaxEntries)
                        _history.RemoveAt(0);
                }
            }
        }
        catch
        {
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_history, new JsonSerializerOptions
            {
                WriteIndented = false
            });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
        }
    }
}
