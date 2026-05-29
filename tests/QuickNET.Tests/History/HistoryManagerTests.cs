using System.Text.Json;
using QuickNET.History;
using QuickNET.Models;

namespace QuickNET.Tests.History;

[TestClass]
public sealed class HistoryManagerTests
{
    private readonly string _tempDir;
    private readonly string _tempPath;

    public HistoryManagerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"QuickNET_Test_{Guid.NewGuid():N}");
        _tempPath = Path.Combine(_tempDir, "history.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [TestMethod]
    public void Constructor_FirstRun_CreatesDirectoryAndEmptyEntries()
    {
        Assert.IsFalse(Directory.Exists(_tempDir));

        var manager = new HistoryManager(_tempPath);

        Assert.IsTrue(Directory.Exists(_tempDir));
        Assert.AreEqual(0, manager.Entries.Count);
    }

    [TestMethod]
    public void AddEntry_SingleEntry_PersistsToDiskImmediately()
    {
        var manager = new HistoryManager(_tempPath);
        var entry = new HistoryEntry
        {
            Language = "CSharp",
            Input = "2 + 2",
            Output = "4",
            IsError = false
        };

        manager.AddEntry(entry);

        Assert.AreEqual(1, manager.Entries.Count);
        Assert.IsTrue(File.Exists(_tempPath));

        var json = File.ReadAllText(_tempPath);
        var loaded = JsonSerializer.Deserialize<List<HistoryEntry>>(json);
        Assert.IsNotNull(loaded);
        Assert.AreEqual(1, loaded!.Count);
        Assert.AreEqual("CSharp", loaded[0].Language);
        Assert.AreEqual("2 + 2", loaded[0].Input);
        Assert.AreEqual("4", loaded[0].Output);
        Assert.IsFalse(loaded[0].IsError);
    }

    [TestMethod]
    public void Load_PreviousEntries_ReloadedBetweenSessions()
    {
        var manager = new HistoryManager(_tempPath);
        manager.AddEntry(new HistoryEntry { Language = "CSharp", Input = "A", Output = "1" });
        manager.AddEntry(new HistoryEntry { Language = "VisualBasic", Input = "B", Output = "2" });

        var manager2 = new HistoryManager(_tempPath);

        Assert.AreEqual(2, manager2.Entries.Count);
        Assert.AreEqual("A", manager2.Entries[0].Input);
        Assert.AreEqual("B", manager2.Entries[1].Input);
    }

    [TestMethod]
    public void Load_CorruptJsonFile_ReturnsEmptyList()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(_tempPath, "this is not valid json");

        var manager = new HistoryManager(_tempPath);

        Assert.AreEqual(0, manager.Entries.Count);
    }

    [TestMethod]
    public void Load_MissingFile_ReturnsEmptyList()
    {
        Assert.IsFalse(File.Exists(_tempPath));

        var manager = new HistoryManager(_tempPath);

        Assert.AreEqual(0, manager.Entries.Count);
    }

    [TestMethod]
    public void AddEntry_ExceedsMaxEntries_RemovesOldest()
    {
        var manager = new HistoryManager(_tempPath, maxEntries: 3);
        manager.AddEntry(new HistoryEntry { Input = "1" });
        manager.AddEntry(new HistoryEntry { Input = "2" });
        manager.AddEntry(new HistoryEntry { Input = "3" });
        manager.AddEntry(new HistoryEntry { Input = "4" });

        Assert.AreEqual(3, manager.Entries.Count);
        Assert.AreEqual("2", manager.Entries[0].Input);
        Assert.AreEqual("3", manager.Entries[1].Input);
        Assert.AreEqual("4", manager.Entries[2].Input);
    }

    [TestMethod]
    public void Clear_RemovesAllEntriesFromMemoryAndDisk()
    {
        var manager = new HistoryManager(_tempPath);
        manager.AddEntry(new HistoryEntry { Input = "test" });
        Assert.AreEqual(1, manager.Entries.Count);
        Assert.IsTrue(File.Exists(_tempPath));
        var jsonBefore = File.ReadAllText(_tempPath);
        Assert.IsTrue(jsonBefore.Contains("test"));

        manager.Clear();

        Assert.AreEqual(0, manager.Entries.Count);

        var jsonAfter = File.ReadAllText(_tempPath);
        Assert.AreEqual("[]", jsonAfter.TrimEnd('\r', '\n'));
    }
}
