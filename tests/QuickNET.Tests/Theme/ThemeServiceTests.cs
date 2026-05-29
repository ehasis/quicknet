using QuickNET.Session;
using QuickNET.Theme;

namespace QuickNET.Tests.Theme;

[TestClass]
public sealed class ThemeServiceTests
{
    private readonly ThemeService _service;
    private readonly SessionState _sessionState;
    private string _tempDir = "";

    public ThemeServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"QuickNET_Test_{Guid.NewGuid():N}");
        var tempFile = Path.Combine(_tempDir, "settings.json");
        _sessionState = new SessionState(tempFile);
        _service = new ThemeService(_sessionState);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [TestMethod]
    public void Constructor_DefaultTheme_IsSystem()
    {
        Assert.AreEqual(AppTheme.System, _service.CurrentTheme);
    }

    [TestMethod]
    public void SetTheme_Light_UpdatesSessionState()
    {
        _service.CurrentTheme = AppTheme.Light;

        Assert.AreEqual("Light", _sessionState.CurrentTheme);
    }

    [TestMethod]
    public void SetTheme_Dark_UpdatesSessionState()
    {
        _service.CurrentTheme = AppTheme.Dark;

        Assert.AreEqual("Dark", _sessionState.CurrentTheme);
    }

    [TestMethod]
    public void SetTheme_System_UpdatesSessionState()
    {
        _service.CurrentTheme = AppTheme.Light;
        _service.CurrentTheme = AppTheme.System;

        Assert.AreEqual("System", _sessionState.CurrentTheme);
    }

    [TestMethod]
    public void SetTheme_FiresThemeChangedEvent()
    {
        AppTheme? received = null;
        _service.ThemeChanged += (_, theme) => received = theme;

        _service.CurrentTheme = AppTheme.Dark;

        Assert.AreEqual(AppTheme.Dark, received);
    }

    [TestMethod]
    public void SetTheme_SameValue_FiresEvent()
    {
        _service.CurrentTheme = AppTheme.Dark;
        AppTheme? received = null;
        _service.ThemeChanged += (_, theme) => received = theme;

        _service.CurrentTheme = AppTheme.Dark;

        Assert.AreEqual(AppTheme.Dark, received);
    }

    [TestMethod]
    public void ParseTheme_Light_ReturnsLight()
    {
        var result = ThemeService.ParseTheme("Light");

        Assert.AreEqual(AppTheme.Light, result);
    }

    [TestMethod]
    public void ParseTheme_Dark_ReturnsDark()
    {
        var result = ThemeService.ParseTheme("dark");

        Assert.AreEqual(AppTheme.Dark, result);
    }

    [TestMethod]
    public void ParseTheme_System_ReturnsSystem()
    {
        var result = ThemeService.ParseTheme("system");

        Assert.AreEqual(AppTheme.System, result);
    }

    [TestMethod]
    public void ParseTheme_Invalid_ReturnsSystem()
    {
        var result = ThemeService.ParseTheme("blue");

        Assert.AreEqual(AppTheme.System, result);
    }

    [TestMethod]
    public void ParseTheme_Null_ReturnsSystem()
    {
        var result = ThemeService.ParseTheme(null);

        Assert.AreEqual(AppTheme.System, result);
    }

    [TestMethod]
    public void ParseTheme_Empty_ReturnsSystem()
    {
        var result = ThemeService.ParseTheme("");

        Assert.AreEqual(AppTheme.System, result);
    }

    [TestMethod]
    public void DetectSystemTheme_DoesNotThrow()
    {
        var result = ThemeService.DetectSystemTheme();

        Assert.IsTrue(result is AppTheme.Light or AppTheme.Dark);
    }
}
