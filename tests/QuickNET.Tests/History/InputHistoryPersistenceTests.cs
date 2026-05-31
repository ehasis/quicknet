using QuickNET.History;

namespace QuickNET.Tests.History;

[TestClass]
public sealed class InputHistoryPersistenceTests
{
    private readonly string _tempDir;
    private readonly string _tempPath;

    public InputHistoryPersistenceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"QuickNET_Test_{Guid.NewGuid():N}");
        _tempPath = Path.Combine(_tempDir, "input-history.json");
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [TestMethod]
    public void Persist_AfterRecord_FileExists()
    {
        var service = new InputHistoryService(_tempPath);
        service.Record("hello");

        Assert.IsTrue(File.Exists(_tempPath));
    }

    [TestMethod]
    public void Persist_RoundTrip_PreservesEntries()
    {
        var service1 = new InputHistoryService(_tempPath);
        service1.Record("a");
        service1.Record("b");

        var service2 = new InputHistoryService(_tempPath);
        var entries = service2.GetEntries();
        Assert.AreEqual(2, entries.Count);
        Assert.AreEqual("a", entries[0]);
        Assert.AreEqual("b", entries[1]);
    }

    [TestMethod]
    public void Persist_Deduplicated_OnSave()
    {
        var service = new InputHistoryService(_tempPath);
        service.Record("same");
        service.Record("same");
        service.Record("same");

        Assert.IsTrue(File.Exists(_tempPath));
        var json = File.ReadAllText(_tempPath);
        var entries = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
        Assert.IsNotNull(entries);
        Assert.AreEqual(1, entries!.Count);
    }
}
