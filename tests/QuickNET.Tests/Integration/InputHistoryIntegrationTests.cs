using QuickNET.History;

namespace QuickNET.Tests.Integration;

[TestClass]
public sealed class InputHistoryIntegrationTests
{
    private string _tempDir = "";

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [TestMethod]
    public void History_PersistsAcrossSessions()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"QuickNET_Test_{Guid.NewGuid():N}");
        var filePath = Path.Combine(_tempDir, "input-history.json");

        var service1 = new InputHistoryService(filePath);
        service1.Record("2+2");

        var service2 = new InputHistoryService(filePath);
        var entries = service2.GetEntries();
        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual("2+2", entries[0]);
    }

    [TestMethod]
    public void History_NavigateAndExecute()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"QuickNET_Test_{Guid.NewGuid():N}");
        var filePath = Path.Combine(_tempDir, "input-history.json");

        var service = new InputHistoryService(filePath);
        service.Record("a");
        service.Record("b");

        Assert.AreEqual("b", service.NavigateOlder(""));
        service.Record("c");
        Assert.IsNull(service.NavigateNewer());
    }

    [TestMethod]
    public void History_DraftRestored_AfterNavigate()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"QuickNET_Test_{Guid.NewGuid():N}");
        var filePath = Path.Combine(_tempDir, "input-history.json");

        var service = new InputHistoryService(filePath);
        service.Record("x");
        service.Record("y");

        service.NavigateOlder("draft");
        service.NavigateOlder("");
        service.NavigateNewer();
        Assert.AreEqual("draft", service.NavigateNewer());
    }
}
