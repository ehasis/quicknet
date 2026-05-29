using Microsoft.Extensions.DependencyInjection;
using QuickNET.App.Models;
using QuickNET.App.ViewModels;
using QuickNET.History;
using QuickNET.Models;
using QuickNET.Session;

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
        services.AddSingleton(new HistoryManager(tempHistoryPath));
        services.AddSingleton(new SessionState(tempSettingsPath));
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
}
