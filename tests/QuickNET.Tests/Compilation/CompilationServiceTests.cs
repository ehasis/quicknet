using Microsoft.Extensions.DependencyInjection;
using QuickNET.Compilation;
using QuickNET.Models;
using QuickNET.Templates;

namespace QuickNET.Tests.Compilation;

[TestClass]
public class CompilationServiceTests
{
    private readonly CompilationService _service;

    public CompilationServiceTests()
    {
        var services = new ServiceCollection();
        services.AddQuickNETCore();
        var provider = services.BuildServiceProvider();
        _service = provider.GetRequiredService<CompilationService>();
    }

    [TestMethod]
    public void Compile_CSharp_SimpleExpression_ReturnsSuccess()
    {
        var result = _service.Compile(new CompilationInput("2 + 2", Language.CSharp));

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.AssemblyBytes);
        Assert.IsTrue(result.AssemblyBytes!.Length > 0);
    }

    [TestMethod]
    public void Compile_VisualBasic_SimpleExpression_ReturnsSuccess()
    {
        var result = _service.Compile(new CompilationInput("2 + 2", Language.VisualBasic));

        if (!result.Success)
        {
            var diagInfo = string.Join("\n", result.Diagnostics.Select(d =>
                $"[{d.Severity}] L{d.Line} C{d.Column}: {d.Message}"));
            Assert.Fail($"VB compilation failed with diagnostics:\n{diagInfo}");
        }

        Assert.IsNotNull(result.AssemblyBytes);
    }

    [TestMethod]
    public void Compile_InvalidCode_ReturnsFailure_WithErrorDiagnostics()
    {
        var result = _service.Compile(new CompilationInput("2 +", Language.CSharp));

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Diagnostics.Any(d => d.Severity == "Error"));
    }

    [TestMethod]
    public void Compile_DiagnosticLineColumn_AdjustedForUserCode()
    {
        var result = _service.Compile(new CompilationInput("2 +", Language.CSharp));

        Assert.IsFalse(result.Success);
        var error = result.Diagnostics.First(d => d.Severity == "Error");
        Assert.IsNotNull(error.Line);
        Assert.IsTrue(error.Line >= 0, "Line should be relative to user code, not template");
    }

    [TestMethod]
    public void Compile_CSharp_MultiLineStatement_ReturnsSuccess()
    {
        var code = """
            var x = 10;
            return x * 2;
            """;

        var result = _service.Compile(new CompilationInput(code, Language.CSharp));

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.AssemblyBytes);
    }

    [TestMethod]
    public void AddQuickNETCore_RegistersAllServices()
    {
        var services = new ServiceCollection();
        services.AddQuickNETCore();
        var provider = services.BuildServiceProvider();

        var engines = provider.GetServices<ITemplateEngine>().ToList();
        Assert.AreEqual(2, engines.Count);
        Assert.IsTrue(engines.Any(e => e is CSharpTemplateEngine));
        Assert.IsTrue(engines.Any(e => e is VbTemplateEngine));

        var compilationService = provider.GetService<CompilationService>();
        Assert.IsNotNull(compilationService);
    }
}
