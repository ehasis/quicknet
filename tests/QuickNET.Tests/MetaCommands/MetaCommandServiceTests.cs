using System.Text.Json;
using QuickNET.Compilation;
using QuickNET.MetaCommands;
using QuickNET.Models;
using QuickNET.Session;

namespace QuickNET.Tests.MetaCommands;

[TestClass]
public sealed class MetaCommandServiceTests
{
    private readonly MetaCommandService _service;
    private readonly SessionState _sessionState;
    private string _tempDir = "";

    public MetaCommandServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"QuickNET_Test_{Guid.NewGuid():N}");
        var tempFile = Path.Combine(_tempDir, "settings.json");
        _sessionState = new SessionState(tempFile);
        _service = new MetaCommandService(_sessionState, new AssemblyResolutionService());
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [TestMethod]
    public void Execute_Help_ReturnsCommandList()
    {
        var result = _service.Execute("/help");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("help", result.Command);
        Assert.Contains("Available commands", result.DisplayText);
        Assert.Contains("/clear", result.DisplayText);
        Assert.Contains("/exit", result.DisplayText);
        Assert.Contains("/help", result.DisplayText);
        Assert.Contains("/lang", result.DisplayText);
        Assert.Contains("/reference", result.DisplayText);
        Assert.Contains("/import", result.DisplayText);
        Assert.Contains("/references", result.DisplayText);
        Assert.Contains("/imports", result.DisplayText);
        Assert.Contains("/timeout", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Clear_ReturnsSuccess()
    {
        var result = _service.Execute("/clear");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("clear", result.Command);
        Assert.Contains("cleared", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Lang_CSharp_SetsLanguage()
    {
        var result = _service.Execute("/lang cs");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(Language.CSharp, _sessionState.CurrentLanguage);
        Assert.Contains("C#", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Lang_VbNet_SetsLanguage()
    {
        var result = _service.Execute("/lang vb");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(Language.VisualBasic, _sessionState.CurrentLanguage);
        Assert.Contains("VB.NET", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Lang_CSharpFull_SetsLanguage()
    {
        var result = _service.Execute("/lang csharp");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(Language.CSharp, _sessionState.CurrentLanguage);
    }

    [TestMethod]
    public void Execute_Lang_VbNetFull_SetsLanguage()
    {
        var result = _service.Execute("/lang vbnet");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(Language.VisualBasic, _sessionState.CurrentLanguage);
    }

    [TestMethod]
    public void Execute_Lang_NoArgs_ShowsCurrent()
    {
        var result = _service.Execute("/lang");

        Assert.IsFalse(result.Success);
        Assert.Contains("Current language", result.DisplayText);
        Assert.Contains("Usage", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Lang_InvalidArg_ReturnsError()
    {
        var result = _service.Execute("/lang python");

        Assert.IsFalse(result.Success);
        Assert.Contains("Unknown language", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Reference_ValidAssembly_AddsReference()
    {
        var result = _service.Execute("/reference System.Text.Json");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, _sessionState.ExtraReferences.Count);
        Assert.AreEqual("System.Text.Json", _sessionState.ExtraReferences[0]);
        Assert.Contains("Added reference", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Reference_InvalidAssembly_ReturnsError()
    {
        var result = _service.Execute("/reference NonExistent.Assembly.Foo");

        Assert.IsFalse(result.Success);
        Assert.Contains("not found", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Reference_NoArgs_ShowsUsage()
    {
        var result = _service.Execute("/reference");

        Assert.IsFalse(result.Success);
        Assert.Contains("Usage", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Import_ValidNamespace_AddsImport()
    {
        var result = _service.Execute("/import System.Text.Json");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, _sessionState.ExtraImports.Count);
        Assert.AreEqual("System.Text.Json", _sessionState.ExtraImports[0]);
        Assert.Contains("Added import", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Import_NoArgs_ShowsUsage()
    {
        var result = _service.Execute("/import");

        Assert.IsFalse(result.Success);
        Assert.Contains("Usage", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Using_Alias_AddsImport()
    {
        var result = _service.Execute("/using System.Text.Json");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(1, _sessionState.ExtraImports.Count);
        Assert.AreEqual("System.Text.Json", _sessionState.ExtraImports[0]);
    }

    [TestMethod]
    public void Execute_References_Empty_ShowsDefaults()
    {
        var result = _service.Execute("/references");

        Assert.IsTrue(result.Success);
        Assert.Contains("Default references", result.DisplayText);
        Assert.Contains("System.Runtime", result.DisplayText);
        Assert.Contains("No extra references", result.DisplayText);
    }

    [TestMethod]
    public void Execute_References_WithExtras_ListsBoth()
    {
        _service.Execute("/reference System.Text.Json");
        var result = _service.Execute("/references");

        Assert.IsTrue(result.Success);
        Assert.Contains("System.Text.Json", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Imports_Empty_ShowsDefaults()
    {
        var result = _service.Execute("/imports");

        Assert.IsTrue(result.Success);
        Assert.Contains("Default imports", result.DisplayText);
        Assert.Contains("System", result.DisplayText);
        Assert.Contains("No extra imports", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Imports_WithExtras_ListsBoth()
    {
        _service.Execute("/import System.Text.Json");
        var result = _service.Execute("/imports");

        Assert.IsTrue(result.Success);
        Assert.Contains("System.Text.Json", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Timeout_ValidValue_SetsTimeout()
    {
        var result = _service.Execute("/timeout 60");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(60, _sessionState.TimeoutSeconds);
        Assert.Contains("60s", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Timeout_Zero_SetsNoLimit()
    {
        var result = _service.Execute("/timeout 0");

        Assert.IsTrue(result.Success);
        Assert.AreEqual(0, _sessionState.TimeoutSeconds);
        Assert.Contains("no limit", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Timeout_NoArgs_ShowsCurrent()
    {
        _sessionState.TimeoutSeconds = 30;
        var result = _service.Execute("/timeout");

        Assert.IsTrue(result.Success);
        Assert.Contains("Current timeout", result.DisplayText);
        Assert.Contains("30s", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Timeout_Negative_ReturnsError()
    {
        var result = _service.Execute("/timeout -5");

        Assert.IsFalse(result.Success);
        Assert.Contains("Invalid", result.DisplayText);
    }

    [TestMethod]
    public void Execute_Timeout_NonNumeric_ReturnsError()
    {
        var result = _service.Execute("/timeout abc");

        Assert.IsFalse(result.Success);
        Assert.Contains("Invalid", result.DisplayText);
    }

    [TestMethod]
    public void Execute_UnknownCommand_ReturnsError()
    {
        var result = _service.Execute("/xyz");

        Assert.IsFalse(result.Success);
        Assert.Contains("Unknown command", result.DisplayText);
        Assert.Contains("/help", result.DisplayText);
    }

    [TestMethod]
    public void Execute_EmptyInput_ReturnsError()
    {
        var result = _service.Execute("");

        Assert.IsFalse(result.Success);
        Assert.AreEqual("", result.Command);
    }

    [TestMethod]
    public void Execute_Exit_ReturnsExitCommand()
    {
        var result = _service.Execute("/exit");

        Assert.IsTrue(result.Success);
        Assert.AreEqual("exit", result.Command);
        Assert.Contains("Goodbye", result.DisplayText);
    }
}
