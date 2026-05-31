using QuickNET.History;

namespace QuickNET.Tests.History;

[TestClass]
public sealed class InputHistoryServiceTests
{
    private readonly string _tempDir;
    private readonly string _tempPath;

    public InputHistoryServiceTests()
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

    private InputHistoryService CreateService()
        => new(_tempPath);

    [TestMethod]
    public void Record_NewInput_AddsToHistory()
    {
        var service = CreateService();
        service.Record("2+2");
        Assert.AreEqual(1, service.Count);
        Assert.AreEqual("2+2", service.GetEntries()[0]);
    }

    [TestMethod]
    public void Record_DuplicateConsecutive_Ignored()
    {
        var service = CreateService();
        service.Record("a");
        service.Record("a");
        Assert.AreEqual(1, service.Count);
    }

    [TestMethod]
    public void Record_SameButNotConsecutive_Added()
    {
        var service = CreateService();
        service.Record("a");
        service.Record("b");
        service.Record("a");
        Assert.AreEqual(3, service.Count);
    }

    [TestMethod]
    public void Record_ExceedsMax_DropsOldest()
    {
        var service = CreateService();
        for (int i = 0; i < 51; i++)
            service.Record($"input{i}");
        Assert.AreEqual(50, service.Count);
        Assert.AreEqual("input1", service.GetEntries()[0]);
    }

    [TestMethod]
    public void Record_WhitespaceOrEmpty_Ignored()
    {
        var service = CreateService();
        service.Record("");
        service.Record("  ");
        Assert.AreEqual(0, service.Count);
    }

    [TestMethod]
    public void NavigateOlder_EmptyHistory_ReturnsNull()
    {
        var service = CreateService();
        Assert.IsNull(service.NavigateOlder("draft"));
    }

    [TestMethod]
    public void NavigateOlder_ReturnsMostRecentEntry()
    {
        var service = CreateService();
        service.Record("1");
        service.Record("2");
        Assert.AreEqual("2", service.NavigateOlder(""));
    }

    [TestMethod]
    public void NavigateOlder_Twice_ReturnsSecondMostRecent()
    {
        var service = CreateService();
        service.Record("1");
        service.Record("2");
        service.Record("3");
        service.NavigateOlder("");
        Assert.AreEqual("2", service.NavigateOlder(""));
    }

    [TestMethod]
    public void NavigateOlder_AtEnd_ReturnsSameEntry()
    {
        var service = CreateService();
        service.Record("only");
        Assert.AreEqual("only", service.NavigateOlder(""));
        Assert.AreEqual("only", service.NavigateOlder(""));
    }

    [TestMethod]
    public void NavigateNewer_AfterOlder_MovesBack()
    {
        var service = CreateService();
        service.Record("1");
        service.Record("2");
        service.Record("3");
        service.NavigateOlder(""); // index 0 → "3"
        service.NavigateOlder(""); // index 1 → "2"
        Assert.AreEqual("3", service.NavigateNewer());
    }

    [TestMethod]
    public void NavigateNewer_AtMostRecent_RestoresDraft()
    {
        var service = CreateService();
        service.Record("1");
        service.Record("2");
        service.NavigateOlder("draft"); // index 0 → "2"
        service.NavigateOlder("");       // index 1 → "1"
        service.NavigateNewer();        // index 0 → "2"
        Assert.AreEqual("draft", service.NavigateNewer());
    }

    [TestMethod]
    public void DraftPreserved_WhenNavigating()
    {
        var service = CreateService();
        service.Record("a");
        service.Record("b");
        service.Record("c");
        Assert.AreEqual("c", service.NavigateOlder("draft123"));
        service.NavigateOlder("");
        service.NavigateNewer();
        Assert.AreEqual("draft123", service.NavigateNewer());
    }

    [TestMethod]
    public void Reset_ExitsNavigation()
    {
        var service = CreateService();
        service.Record("a");
        service.NavigateOlder("x");
        service.Reset();
        Assert.IsNull(service.NavigateNewer());
    }

    [TestMethod]
    public void Reset_ClearsDraft()
    {
        var service = CreateService();
        service.Record("a");
        service.NavigateOlder("draft");
        service.Reset();
        Assert.IsNull(service.NavigateNewer());
    }

    [TestMethod]
    public void Record_ResetsNavigation()
    {
        var service = CreateService();
        service.Record("a");
        service.NavigateOlder("x");
        service.Record("new");
        Assert.IsNull(service.NavigateNewer());
    }

    [TestMethod]
    public void Load_ExistingFile_LoadsHistory()
    {
        var json = "[\"a\",\"b\",\"c\"]";
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(_tempPath, json);

        var service = CreateService();
        var entries = service.GetEntries();
        Assert.AreEqual(3, entries.Count);
        Assert.AreEqual("a", entries[0]);
        Assert.AreEqual("b", entries[1]);
        Assert.AreEqual("c", entries[2]);
    }

    [TestMethod]
    public void Load_MissingFile_EmptyHistory()
    {
        var service = CreateService();
        Assert.AreEqual(0, service.Count);
    }

    [TestMethod]
    public void Load_CorruptFile_EmptyHistory()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(_tempPath, "not valid json {{{");

        var service = CreateService();
        Assert.AreEqual(0, service.Count);
    }

    [TestMethod]
    public void Save_CreatesFile()
    {
        var service = CreateService();
        service.Record("test");

        Assert.IsTrue(File.Exists(_tempPath));
        var content = File.ReadAllText(_tempPath);
        Assert.Contains("test", content);
    }

    [TestMethod]
    public void Save_MaxEntriesInFile()
    {
        var service = CreateService();
        for (int i = 0; i < 51; i++)
            service.Record($"input{i}");

        Assert.IsTrue(File.Exists(_tempPath));
        var json = File.ReadAllText(_tempPath);
        var entries = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
        Assert.IsNotNull(entries);
        Assert.AreEqual(50, entries!.Count);
    }
}
