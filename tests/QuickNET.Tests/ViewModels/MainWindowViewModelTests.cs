using Microsoft.Extensions.DependencyInjection;
using QuickNET.App.Models;
using QuickNET.App.ViewModels;
using QuickNET.History;
using QuickNET.Models;
using QuickNET.Session;
using QuickNET.Theme;

namespace QuickNET.Tests.ViewModels;

[TestClass]
public sealed class MainWindowViewModelTests
{
    private readonly ServiceProvider _provider;
    private readonly string _tempDir;

    public MainWindowViewModelTests()
    {
        var services = new ServiceCollection();
        services.AddQuickNETCore();
        _tempDir = Path.Combine(Path.GetTempPath(), $"QuickNET_Test_{Guid.NewGuid():N}");
        var tempHistoryPath = Path.Combine(_tempDir, "history.json");
        var tempSettingsPath = Path.Combine(_tempDir, "settings.json");
        var tempInputHistoryPath = Path.Combine(_tempDir, "input-history.json");
        services.AddSingleton(new HistoryManager(tempHistoryPath));
        services.AddSingleton(new InputHistoryService(tempInputHistoryPath));
        var sessionState = new SessionState(tempSettingsPath);
        services.AddSingleton(sessionState);
        services.AddSingleton(new ThemeService(sessionState));
        services.AddSingleton<HistoryService>();
        services.AddSingleton<MainWindowViewModel>();
        _provider = services.BuildServiceProvider();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _provider.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [TestMethod]
    public void ExecuteCode_ValidInput_AddsConversationItems()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        vm.InputText = "2 + 2";

        vm.ExecuteCodeCommand.Execute(null);

        Assert.IsTrue(vm.ConversationItems.Count >= 2);
        Assert.AreEqual("> 2 + 2", vm.ConversationItems[^2].DisplayText);
        Assert.AreEqual("4", vm.ConversationItems[^1].DisplayText);
        Assert.IsFalse(vm.ConversationItems[^1].IsError);
    }

    [TestMethod]
    public void ExecuteCode_InvalidInput_AddsErrorItem()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        vm.InputText = "2 +";

        vm.ExecuteCodeCommand.Execute(null);

        Assert.IsTrue(vm.ConversationItems.Count >= 2);
        Assert.IsTrue(vm.ConversationItems[^1].IsError);
    }

    [TestMethod]
    public void ExecuteCode_EmptyInput_NoOp()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        var initialCount = vm.ConversationItems.Count;
        vm.InputText = "";

        vm.ExecuteCodeCommand.Execute(null);

        Assert.AreEqual(initialCount, vm.ConversationItems.Count);
    }

    [TestMethod]
    public void ClearHistory_RemovesAllItems()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        vm.InputText = "2 + 2";
        vm.ExecuteCodeCommand.Execute(null);
        Assert.IsTrue(vm.ConversationItems.Count > 0);

        vm.ClearHistoryCommand.Execute(null);

        Assert.AreEqual(0, vm.ConversationItems.Count);
    }

    [TestMethod]
    public void SelectedLanguageIndex_DefaultIsCSharp()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();

        Assert.AreEqual(0, vm.SelectedLanguageIndex);
    }

    [TestMethod]
    public void LanguageSwitch_VbNet_ExecutesCorrectly()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        vm.SelectedLanguageIndex = 1;
        vm.InputText = "2 + 2";

        vm.ExecuteCodeCommand.Execute(null);

        Assert.IsTrue(vm.ConversationItems.Count >= 2);
        Assert.AreEqual("4", vm.ConversationItems[^1].DisplayText);
        Assert.IsFalse(vm.ConversationItems[^1].IsError);
    }

    [TestMethod]
    public void ExecuteCode_MetaCommand_Help_DisplaysInConversation()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        vm.InputText = "/help";

        vm.ExecuteCodeCommand.Execute(null);

        Assert.IsTrue(vm.ConversationItems.Count >= 2);
        Assert.AreEqual("> /help", vm.ConversationItems[^2].DisplayText);
        Assert.IsFalse(vm.ConversationItems[^1].IsError);
        Assert.Contains("Available commands", vm.ConversationItems[^1].DisplayText);
    }

    [TestMethod]
    public void ExecuteCode_MetaCommand_Clear_ClearsPanel()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        vm.InputText = "2 + 2";
        vm.ExecuteCodeCommand.Execute(null);
        Assert.IsTrue(vm.ConversationItems.Count > 0);

        vm.InputText = "/clear";
        vm.ExecuteCodeCommand.Execute(null);

        Assert.AreEqual(1, vm.ConversationItems.Count);
        Assert.Contains("cleared", vm.ConversationItems[0].DisplayText);
    }

    [TestMethod]
    public void ExecuteCode_MetaCommand_Lang_UpdatesLanguage()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        vm.SelectedLanguageIndex = 0;
        vm.InputText = "/lang vb";

        vm.ExecuteCodeCommand.Execute(null);

        Assert.AreEqual(1, vm.SelectedLanguageIndex);
    }

    [TestMethod]
    public void ExecuteCode_MetaCommand_Lang_SyncsToSessionState()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        var sessionState = _provider.GetRequiredService<SessionState>();
        vm.InputText = "/lang vb";

        vm.ExecuteCodeCommand.Execute(null);

        Assert.AreEqual(Language.VisualBasic, sessionState.CurrentLanguage);
    }

    [TestMethod]
    public void ExecuteCode_MetaCommand_Unknown_ShowsError()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        vm.InputText = "/xyz";

        vm.ExecuteCodeCommand.Execute(null);

        Assert.IsTrue(vm.ConversationItems.Count >= 2);
        Assert.IsTrue(vm.ConversationItems[^1].IsError);
        Assert.Contains("Unknown command", vm.ConversationItems[^1].DisplayText);
    }

    [TestMethod]
    public void StatusText_AfterSuccessfulMetaCommand_IsReady()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        vm.InputText = "/help";

        vm.ExecuteCodeCommand.Execute(null);

        Assert.AreEqual("Ready", vm.StatusText);
    }

    [TestMethod]
    public void StatusText_AfterFailedMetaCommand_IsError()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        vm.InputText = "/xyz";

        vm.ExecuteCodeCommand.Execute(null);

        Assert.AreEqual("Error", vm.StatusText);
    }

    [TestMethod]
    public void SessionInfoText_ShowsLanguageTimeoutRefsImports()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        var sessionState = _provider.GetRequiredService<SessionState>();
        sessionState.AddReference("System.Text.Json");
        sessionState.AddImport("System.Text.Json");

        var info = vm.SessionInfoText;

        Assert.Contains("C#", info);
        Assert.Contains("Timeout:", info);
        Assert.Contains("Refs: 1", info);
        Assert.Contains("Imports: 1", info);
    }

    [TestMethod]
    public void RestoreSessionSettings_RestoresLanguage()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"QuickNET_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var tempSessionPath = Path.Combine(tempDir, "settings.json");
            var session = new SessionState(tempSessionPath);
            session.CurrentLanguage = Language.VisualBasic;

            var services = new ServiceCollection();
            services.AddQuickNETCore();
            var tempHistoryPath = Path.Combine(tempDir, "history.json");
            services.AddSingleton(new HistoryManager(tempHistoryPath));
            services.AddSingleton(session);
            services.AddSingleton(new ThemeService(session));
            services.AddSingleton<HistoryService>();
            services.AddSingleton<MainWindowViewModel>();
            using var provider = services.BuildServiceProvider();

            var vm = provider.GetRequiredService<MainWindowViewModel>();
            Assert.AreEqual(1, vm.SelectedLanguageIndex);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    [TestMethod]
    public void SessionInfoText_DefaultTheme_NoThemeLabel()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();

        var info = vm.SessionInfoText;

        Assert.DoesNotContain("Dark |", info);
        Assert.DoesNotContain("Light |", info);
    }

    [TestMethod]
    public void SessionInfoText_DarkTheme_ContainsLabel()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        vm.InputText = "/theme dark";
        vm.ExecuteCodeCommand.Execute(null);

        var info = vm.SessionInfoText;

        Assert.Contains("Dark |", info);
    }

    [TestMethod]
    public void SessionInfoText_LightTheme_ContainsLabel()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        vm.InputText = "/theme light";
        vm.ExecuteCodeCommand.Execute(null);

        var info = vm.SessionInfoText;

        Assert.Contains("Light |", info);
    }

    [TestMethod]
    public void ExecuteCode_RecordsInputInHistory()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        vm.InputText = "2 + 2";

        vm.ExecuteCodeCommand.Execute(null);

        var inputHistory = _provider.GetRequiredService<InputHistoryService>();
        var entries = inputHistory.GetEntries();
        Assert.AreEqual(1, entries.Count);
        Assert.AreEqual("2 + 2", entries[0]);
    }

    [TestMethod]
    public void ExecuteCode_MetaCommand_NotRecordedInInputHistory()
    {
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        vm.InputText = "/help";

        vm.ExecuteCodeCommand.Execute(null);

        var inputHistory = _provider.GetRequiredService<InputHistoryService>();
        Assert.AreEqual(0, inputHistory.Count);
    }

    [TestMethod]
    public void NavigateHistoryOlder_UpdatesInputText()
    {
        var inputHistory = _provider.GetRequiredService<InputHistoryService>();
        inputHistory.Record("prev");
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        vm.InputText = "";

        vm.NavigateHistoryOlder();

        Assert.AreEqual("prev", vm.InputText);
    }

    [TestMethod]
    public void NavigateHistoryNewer_AfterOlder_RestoresDraft()
    {
        var inputHistory = _provider.GetRequiredService<InputHistoryService>();
        inputHistory.Record("a");
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        vm.InputText = "draft";

        vm.NavigateHistoryOlder();
        vm.NavigateHistoryNewer();

        Assert.AreEqual("draft", vm.InputText);
    }

    [TestMethod]
    public void ResetHistoryNavigation_AfterNavigate_Resets()
    {
        var inputHistory = _provider.GetRequiredService<InputHistoryService>();
        inputHistory.Record("a");
        var vm = _provider.GetRequiredService<MainWindowViewModel>();
        vm.InputText = "x";

        vm.NavigateHistoryOlder();
        vm.ResetHistoryNavigation();

        Assert.IsNull(inputHistory.NavigateNewer());
    }
}
