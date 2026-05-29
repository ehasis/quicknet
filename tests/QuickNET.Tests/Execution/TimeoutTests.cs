using Microsoft.Extensions.DependencyInjection;
using QuickNET.Compilation;
using QuickNET.Execution;
using QuickNET.Models;
using QuickNET.Session;
using QuickNET.Templates;

namespace QuickNET.Tests.Execution;

[TestClass]
public sealed class TimeoutTests
{
    private readonly ExecutionService _execution;
    private readonly CompilationService _compilation;
    private readonly string _tempDir;

    public TimeoutTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITemplateEngine, CSharpTemplateEngine>();
        services.AddSingleton<ITemplateEngine, VbTemplateEngine>();
        services.AddSingleton<AssemblyResolutionService>();
        services.AddSingleton<CompilationService>();
        services.AddSingleton<ExecutionService>();
        var provider = services.BuildServiceProvider();
        _compilation = provider.GetRequiredService<CompilationService>();
        _execution = provider.GetRequiredService<ExecutionService>();

        _tempDir = Path.Combine(Path.GetTempPath(), $"QuickNET_Test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [TestMethod]
    public void Execute_WithAdequateTimeout_ReturnsSuccess()
    {
        var compilation = _compilation.Compile(new CompilationInput("2 + 2", Language.CSharp));
        Assert.IsTrue(compilation.Success);

        var result = _execution.Execute(new ExecutionInput(compilation.AssemblyBytes!), 30);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("4", result.Output);
    }

    [TestMethod]
    public void Execute_WithZeroTimeout_SkipsTimeout()
    {
        var compilation = _compilation.Compile(new CompilationInput("2 + 2", Language.CSharp));
        Assert.IsTrue(compilation.Success);

        var result = _execution.Execute(new ExecutionInput(compilation.AssemblyBytes!), 0);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("4", result.Output);
    }

    [TestMethod]
    public void Execute_WithExpiredTimeout_ReturnsTimeoutError()
    {
        var compilation = _compilation.Compile(new CompilationInput(
            "System.Threading.Thread.Sleep(2000); return 42;", Language.CSharp));
        Assert.IsTrue(compilation.Success);

        var result = _execution.Execute(new ExecutionInput(compilation.AssemblyBytes!), 1);

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.Contains("timed out after 1", result.Error);
    }

    [TestMethod]
    public void Execute_Timeout_ReturnsErrorWithCorrectMessage()
    {
        var compilation = _compilation.Compile(new CompilationInput(
            "System.Threading.Thread.Sleep(5000); return 42;", Language.CSharp));
        Assert.IsTrue(compilation.Success);

        var result = _execution.Execute(new ExecutionInput(compilation.AssemblyBytes!), 1);

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        Assert.Contains("timed out after 1", result.Error);
    }

    [TestMethod]
    public void ReplEngine_UsesSessionTimeout()
    {
        var tempSettingsPath = Path.Combine(_tempDir, "settings.json");
        var sessionState = new SessionState(tempSettingsPath);
        sessionState.TimeoutSeconds = 5;

        var replEngine = new ReplEngine(_compilation, _execution, sessionState);

        var result = replEngine.Execute("2 + 2", Language.CSharp);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("4", result.Output);
    }
}
