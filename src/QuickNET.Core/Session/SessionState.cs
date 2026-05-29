using System.Diagnostics;
using System.Text.Json;
using QuickNET.Models;

namespace QuickNET.Session;

public class SessionState
{
    private readonly string _filePath;
    private SessionSettings _settings = new();

    public IReadOnlyList<string> ExtraReferences => _settings.ExtraReferences.AsReadOnly();
    public IReadOnlyList<string> ExtraImports => _settings.ExtraImports.AsReadOnly();

    public int TimeoutSeconds
    {
        get => _settings.TimeoutSeconds;
        set { _settings.TimeoutSeconds = value; Save(); }
    }

    public Language CurrentLanguage
    {
        get => Enum.TryParse<Language>(_settings.Language, out var lang) ? lang : Language.CSharp;
        set { _settings.Language = value.ToString(); Save(); }
    }

    public SessionState()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(appData, "QuickNET");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "settings.json");
        Load();
    }

    internal SessionState(string filePath)
    {
        _filePath = filePath;
        var directory = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(directory);
        Load();
    }

    public void AddReference(string assemblyName)
    {
        if (_settings.ExtraReferences.Contains(assemblyName, StringComparer.OrdinalIgnoreCase))
            return;
        _settings.ExtraReferences.Add(assemblyName);
        Save();
    }

    public bool RemoveReference(string assemblyName)
    {
        var index = _settings.ExtraReferences.FindIndex(
            r => string.Equals(r, assemblyName, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return false;
        _settings.ExtraReferences.RemoveAt(index);
        Save();
        return true;
    }

    public void AddImport(string namespaceName)
    {
        if (_settings.ExtraImports.Contains(namespaceName, StringComparer.OrdinalIgnoreCase))
            return;
        _settings.ExtraImports.Add(namespaceName);
        Save();
    }

    public bool RemoveImport(string namespaceName)
    {
        var index = _settings.ExtraImports.FindIndex(
            i => string.Equals(i, namespaceName, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return false;
        _settings.ExtraImports.RemoveAt(index);
        Save();
        return true;
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var loaded = JsonSerializer.Deserialize<SessionSettings>(json, options);
                if (loaded != null)
                    _settings = loaded;
            }
            else
            {
                Save();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load settings from {_filePath}: {ex.Message}");
            Save();
        }
    }

    private void Save()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var json = JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(_filePath, json);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save settings to {_filePath}: {ex.Message}");
        }
    }
}
