using System.Diagnostics;
using System.Text.Json;
using QuickNET.Models;

namespace QuickNET.History;

public class HistoryManager
{
    private readonly string _filePath;
    private readonly int _maxEntries;
    private readonly JsonSerializerOptions _jsonOptions;
    private List<HistoryEntry> _entries = [];

    public IReadOnlyList<HistoryEntry> Entries => _entries.AsReadOnly();

    public HistoryManager(int maxEntries = 500)
        : this(GetDefaultFilePath(), maxEntries)
    {
    }

    public HistoryManager(string historyPath, int maxEntries = 500)
    {
        _maxEntries = maxEntries;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _filePath = historyPath;
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null)
            Directory.CreateDirectory(directory);
        Load();
    }

    private static string GetDefaultFilePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "QuickNET", "history.json");
    }

    public void AddEntry(HistoryEntry entry)
    {
        _entries.Add(entry);
        while (_entries.Count > _maxEntries)
            _entries.RemoveAt(0);
        Save();
    }

    public void Clear()
    {
        _entries.Clear();
        Save();
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return;

            var json = File.ReadAllText(_filePath);
            var loaded = JsonSerializer.Deserialize<List<HistoryEntry>>(json, _jsonOptions);
            if (loaded is not null)
                _entries = loaded;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load history: {ex.Message}");
            _entries = [];
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_entries, _jsonOptions);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save history: {ex.Message}");
        }
    }
}
