using Microsoft.Extensions.DependencyInjection;
using QuickNET.App.ViewModels;
using QuickNET.Completion;
using QuickNET.History;
using QuickNET.MetaCommands;
using QuickNET.Theme;

namespace QuickNET.Tests.Integration;

[TestClass]
public sealed class DIIntegrationTests
{
    [TestMethod]
    public void AllServices_Resolve_WithoutError()
    {
        var services = new ServiceCollection();
        services.AddQuickNETCore();
        services.AddSingleton<MainWindowViewModel>();
        var provider = services.BuildServiceProvider();

        Assert.IsNotNull(provider.GetService<ReplEngine>());
        Assert.IsNotNull(provider.GetService<ThemeService>());
        Assert.IsNotNull(provider.GetService<CompletionEngine>());
        Assert.IsNotNull(provider.GetService<InputHistoryService>());
        Assert.IsNotNull(provider.GetService<MainWindowViewModel>());
    }

    [TestMethod]
    public void MetaCommandService_HasThemeServiceDependency()
    {
        var services = new ServiceCollection();
        services.AddQuickNETCore();
        var provider = services.BuildServiceProvider();

        var metaService = provider.GetRequiredService<MetaCommandService>();
        var result = metaService.Execute("/theme dark");
        Assert.IsTrue(result.Success);
        Assert.Contains("Dark", result.DisplayText);
    }

    [TestMethod]
    public void CompositeDependencies_AllResolved()
    {
        var services = new ServiceCollection();
        services.AddQuickNETCore();
        services.AddSingleton<MainWindowViewModel>();
        var provider = services.BuildServiceProvider();

        var engine = provider.GetRequiredService<ReplEngine>();
        Assert.IsNotNull(engine);

        var vm = provider.GetRequiredService<MainWindowViewModel>();
        Assert.IsNotNull(vm);

        var completion = provider.GetRequiredService<CompletionEngine>();
        Assert.IsNotNull(completion);

        var inputHistory = provider.GetRequiredService<InputHistoryService>();
        Assert.IsNotNull(inputHistory);

        var theme = provider.GetRequiredService<ThemeService>();
        Assert.IsNotNull(theme);
    }
}
