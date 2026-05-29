using QuickNET.History;
using QuickNET.Models;

namespace QuickNET.Tests.History;

[TestClass]
public sealed class HistoryServiceTests
{
    private readonly string _tempDir;
    private readonly string _tempPath;

    public HistoryServiceTests()
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
    public void Record_CreatesEntryWithCorrectValues()
    {
        var manager = new HistoryManager(_tempPath);
        var service = new HistoryService(manager);

        service.Record("2 + 2", "CSharp", "4", false);

        var entries = service.GetEntries();
        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual("2 + 2", entries[0].Input);
        Assert.AreEqual("CSharp", entries[0].Language);
        Assert.AreEqual("4", entries[0].Output);
        Assert.IsFalse(entries[0].IsError);
        Assert.IsTrue((DateTime.Now - entries[0].Timestamp).TotalSeconds < 5);
    }

    [TestMethod]
    public void Record_ErrorEntry_SetsIsErrorTrue()
    {
        var manager = new HistoryManager(_tempPath);
        var service = new HistoryService(manager);

        service.Record("x", "CSharp", "Compilation error: ...", true);

        var entries = service.GetEntries();
        Assert.AreEqual(1, entries.Count);
        Assert.IsTrue(entries[0].IsError);
    }

    [TestMethod]
    public void GetEntries_ReturnsReadOnlyList()
    {
        var manager = new HistoryManager(_tempPath);
        var service = new HistoryService(manager);
        service.Record("A", "CSharp", "1", false);
        service.Record("B", "CSharp", "2", false);

        var entries = service.GetEntries();

        Assert.AreEqual(2, entries.Count);
    }

    [TestMethod]
    public void Clear_DelegatesToManager()
    {
        var manager = new HistoryManager(_tempPath);
        var service = new HistoryService(manager);
        service.Record("test", "CSharp", "out", false);
        Assert.AreEqual(1, service.GetEntries().Count);

        service.Clear();

        Assert.AreEqual(0, service.GetEntries().Count);
    }

    [TestMethod]
    public void Record_MultipleLanguages_PreservesLanguagePerEntry()
    {
        var manager = new HistoryManager(_tempPath);
        var service = new HistoryService(manager);

        service.Record("2 + 2", "CSharp", "4", false);
        service.Record("2 + 2", "VisualBasic", "4", false);

        var entries = service.GetEntries();
        Assert.AreEqual(2, entries.Count);
        Assert.AreEqual("CSharp", entries[0].Language);
        Assert.AreEqual("VisualBasic", entries[1].Language);
    }
}
