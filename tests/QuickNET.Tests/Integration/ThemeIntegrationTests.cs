using QuickNET.Compilation;
using QuickNET.MetaCommands;
using QuickNET.Models;
using QuickNET.Session;
using QuickNET.Theme;

namespace QuickNET.Tests.Integration;

[TestClass]
public sealed class ThemeIntegrationTests
{
    private string _tempDir = "";

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [TestMethod]
    public void FullThemeLifecycle_DefaultToDarkAndBack()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"QuickNET_Test_{Guid.NewGuid():N}");
        var settingsPath = Path.Combine(_tempDir, "settings.json");

        var sessionState = new SessionState(settingsPath);
        var themeService = new ThemeService(sessionState);
        var metaService = new MetaCommandService(sessionState, new AssemblyResolutionService(), themeService);

        Assert.AreEqual(AppTheme.System, themeService.CurrentTheme);

        var result = metaService.Execute("/theme dark");
        Assert.IsTrue(result.Success);
        Assert.AreEqual("Dark", sessionState.CurrentTheme);
        Assert.AreEqual(AppTheme.Dark, themeService.CurrentTheme);

        var sessionState2 = new SessionState(settingsPath);
        var themeService2 = new ThemeService(sessionState2);
        Assert.AreEqual(AppTheme.Dark, themeService2.CurrentTheme);

        var metaService2 = new MetaCommandService(sessionState2, new AssemblyResolutionService(), themeService2);
        var result2 = metaService2.Execute("/theme light");
        Assert.IsTrue(result2.Success);
        Assert.AreEqual("Light", sessionState2.CurrentTheme);

        var sessionState3 = new SessionState(settingsPath);
        var themeService3 = new ThemeService(sessionState3);
        Assert.AreEqual(AppTheme.Light, themeService3.CurrentTheme);
    }

    [TestMethod]
    public void ThemePersisted_AfterRestart()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"QuickNET_Test_{Guid.NewGuid():N}");
        var settingsPath = Path.Combine(_tempDir, "settings.json");

        var session1 = new SessionState(settingsPath);
        var theme1 = new ThemeService(session1);
        var meta1 = new MetaCommandService(session1, new AssemblyResolutionService(), theme1);
        meta1.Execute("/theme dark");

        var session2 = new SessionState(settingsPath);
        var theme2 = new ThemeService(session2);
        Assert.AreEqual(AppTheme.Dark, theme2.CurrentTheme);
    }
}
