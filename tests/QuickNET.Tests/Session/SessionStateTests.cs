using System.Text.Json;
using QuickNET.Models;
using QuickNET.Session;

namespace QuickNET.Tests.Session;

[TestClass]
public sealed class SessionStateTests
{
    private string _tempDir = "";

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private SessionState CreateSessionState()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"QuickNET_Test_{Guid.NewGuid():N}");
        var tempFile = Path.Combine(_tempDir, "settings.json");
        return new SessionState(tempFile);
    }

    [TestMethod]
    public void Constructor_FirstRun_CreatesSettingsFile()
    {
        var state = CreateSessionState();

        Assert.IsTrue(File.Exists(Path.Combine(_tempDir, "settings.json")));
        var json = File.ReadAllText(Path.Combine(_tempDir, "settings.json"));
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var settings = JsonSerializer.Deserialize<SessionSettings>(json, options);
        Assert.IsNotNull(settings);
        Assert.AreEqual(30, settings.TimeoutSeconds);
        Assert.AreEqual("CSharp", settings.Language);
    }

    [TestMethod]
    public void Constructor_ExistingFile_LoadsSettings()
    {
        var existingSettings = new SessionSettings
        {
            TimeoutSeconds = 60,
            Language = "VisualBasic",
            ExtraReferences = ["System.Text.Json"],
            ExtraImports = ["System.Text.Json"]
        };
        _tempDir = Path.Combine(Path.GetTempPath(), $"QuickNET_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        var tempFile = Path.Combine(_tempDir, "settings.json");
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        File.WriteAllText(tempFile, JsonSerializer.Serialize(existingSettings, options));

        var state = new SessionState(tempFile);

        Assert.AreEqual(60, state.TimeoutSeconds);
        Assert.AreEqual(Language.VisualBasic, state.CurrentLanguage);
        Assert.AreEqual(1, state.ExtraReferences.Count);
        Assert.AreEqual("System.Text.Json", state.ExtraReferences[0]);
        Assert.AreEqual(1, state.ExtraImports.Count);
        Assert.AreEqual("System.Text.Json", state.ExtraImports[0]);
    }

    [TestMethod]
    public void Constructor_CorruptFile_FallsBackToDefaults()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"QuickNET_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        var tempFile = Path.Combine(_tempDir, "settings.json");
        File.WriteAllText(tempFile, "this is not valid json {{{");

        var state = new SessionState(tempFile);

        Assert.AreEqual(30, state.TimeoutSeconds);
        Assert.AreEqual(Language.CSharp, state.CurrentLanguage);
        Assert.AreEqual(0, state.ExtraReferences.Count);
    }

    [TestMethod]
    public void CurrentLanguage_Setter_PersistsImmediately()
    {
        var state = CreateSessionState();
        state.CurrentLanguage = Language.VisualBasic;

        var json = File.ReadAllText(Path.Combine(_tempDir, "settings.json"));
        Assert.Contains("visualbasic", json.ToLowerInvariant());
    }

    [TestMethod]
    public void TimeoutSeconds_Setter_PersistsImmediately()
    {
        var state = CreateSessionState();
        state.TimeoutSeconds = 10;

        var json = File.ReadAllText(Path.Combine(_tempDir, "settings.json"));
        Assert.Contains("timeoutseconds", json.ToLowerInvariant());
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var settings = JsonSerializer.Deserialize<SessionSettings>(json, options);
        Assert.IsNotNull(settings);
        Assert.AreEqual(10, settings.TimeoutSeconds);
    }

    [TestMethod]
    public void AddReference_NewReference_AddsAndPersists()
    {
        var state = CreateSessionState();
        state.AddReference("System.Text.Json");

        Assert.AreEqual(1, state.ExtraReferences.Count);
        Assert.AreEqual("System.Text.Json", state.ExtraReferences[0]);

        var json = File.ReadAllText(Path.Combine(_tempDir, "settings.json"));
        Assert.Contains("System.Text.Json", json);
    }

    [TestMethod]
    public void AddReference_DuplicateReference_Ignored()
    {
        var state = CreateSessionState();
        state.AddReference("System.Text.Json");
        state.AddReference("System.Text.Json");

        Assert.AreEqual(1, state.ExtraReferences.Count);
    }

    [TestMethod]
    public void AddReference_DuplicateReferenceCaseInsensitive_Ignored()
    {
        var state = CreateSessionState();
        state.AddReference("System.Text.Json");
        state.AddReference("system.text.json");

        Assert.AreEqual(1, state.ExtraReferences.Count);
    }

    [TestMethod]
    public void RemoveReference_Existing_RemovesAndReturnsTrue()
    {
        var state = CreateSessionState();
        state.AddReference("A");
        var removed = state.RemoveReference("A");

        Assert.IsTrue(removed);
        Assert.AreEqual(0, state.ExtraReferences.Count);
    }

    [TestMethod]
    public void RemoveReference_CaseInsensitive_RemovesAndReturnsTrue()
    {
        var state = CreateSessionState();
        state.AddReference("System.Text.Json");
        var removed = state.RemoveReference("system.text.json");

        Assert.IsTrue(removed);
        Assert.AreEqual(0, state.ExtraReferences.Count);
    }

    [TestMethod]
    public void RemoveReference_NonExisting_ReturnsFalse()
    {
        var state = CreateSessionState();
        var removed = state.RemoveReference("B");

        Assert.IsFalse(removed);
    }

    [TestMethod]
    public void AddImport_NewNamespace_AddsAndPersists()
    {
        var state = CreateSessionState();
        state.AddImport("System.Text.Json");

        Assert.AreEqual(1, state.ExtraImports.Count);
        Assert.AreEqual("System.Text.Json", state.ExtraImports[0]);

        var json = File.ReadAllText(Path.Combine(_tempDir, "settings.json"));
        Assert.Contains("System.Text.Json", json);
    }

    [TestMethod]
    public void AddImport_DuplicateNamespace_Ignored()
    {
        var state = CreateSessionState();
        state.AddImport("System.Text.Json");
        state.AddImport("System.Text.Json");

        Assert.AreEqual(1, state.ExtraImports.Count);
    }

    [TestMethod]
    public void RemoveImport_Existing_RemovesAndReturnsTrue()
    {
        var state = CreateSessionState();
        state.AddImport("System.Text.Json");
        var removed = state.RemoveImport("System.Text.Json");

        Assert.IsTrue(removed);
        Assert.AreEqual(0, state.ExtraImports.Count);
    }

    [TestMethod]
    public void RemoveImport_CaseInsensitive_RemovesAndReturnsTrue()
    {
        var state = CreateSessionState();
        state.AddImport("System.Text.Json");
        var removed = state.RemoveImport("system.text.json");

        Assert.IsTrue(removed);
        Assert.AreEqual(0, state.ExtraImports.Count);
    }

    [TestMethod]
    public void RemoveImport_NonExisting_ReturnsFalse()
    {
        var state = CreateSessionState();
        var removed = state.RemoveImport("NonExistent");

        Assert.IsFalse(removed);
    }

    [TestMethod]
    public void ExtraReferences_ReturnsReadOnlyList()
    {
        var state = CreateSessionState();
        var refs = state.ExtraReferences;

        Assert.IsInstanceOfType<IReadOnlyList<string>>(refs);
    }

    [TestMethod]
    public void ExtraImports_ReturnsReadOnlyList()
    {
        var state = CreateSessionState();
        var imports = state.ExtraImports;

        Assert.IsInstanceOfType<IReadOnlyList<string>>(imports);
    }

    [TestMethod]
    public void CurrentTheme_Default_IsSystem()
    {
        var state = CreateSessionState();

        Assert.AreEqual("System", state.CurrentTheme);
    }

    [TestMethod]
    public void CurrentTheme_Setter_PersistsImmediately()
    {
        var state = CreateSessionState();
        state.CurrentTheme = "Dark";

        var json = File.ReadAllText(Path.Combine(_tempDir, "settings.json"));
        Assert.Contains("dark", json.ToLowerInvariant());
    }

    [TestMethod]
    public void CurrentTheme_LoadsFromExistingFile()
    {
        var existingSettings = new SessionSettings
        {
            Theme = "Light"
        };
        _tempDir = Path.Combine(Path.GetTempPath(), $"QuickNET_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        var tempFile = Path.Combine(_tempDir, "settings.json");
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        File.WriteAllText(tempFile, JsonSerializer.Serialize(existingSettings, options));

        var state = new SessionState(tempFile);

        Assert.AreEqual("Light", state.CurrentTheme);
    }

    [TestMethod]
    public void CurrentTheme_DeserializeMissing_DefaultsToSystem()
    {
        var existingSettings = new SessionSettings
        {
            Language = "VisualBasic"
        };
        _tempDir = Path.Combine(Path.GetTempPath(), $"QuickNET_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        var tempFile = Path.Combine(_tempDir, "settings.json");
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        File.WriteAllText(tempFile, JsonSerializer.Serialize(existingSettings, options));

        var state = new SessionState(tempFile);

        Assert.AreEqual("System", state.CurrentTheme);
    }
}
