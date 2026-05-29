using Microsoft.Extensions.DependencyInjection;
using QuickNET.Compilation;
using QuickNET.Models;
using QuickNET.Templates;

namespace QuickNET.Tests.Compilation;

[TestClass]
public sealed class DynamicCompilationTests
{
    private readonly CompilationService _compilation;

    public DynamicCompilationTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITemplateEngine, CSharpTemplateEngine>();
        services.AddSingleton<ITemplateEngine, VbTemplateEngine>();
        services.AddSingleton<AssemblyResolutionService>();
        services.AddSingleton<CompilationService>();
        var provider = services.BuildServiceProvider();
        _compilation = provider.GetRequiredService<CompilationService>();
    }

    [TestMethod]
    public void Compile_WithExtraReference_UsesAssembly()
    {
        var input = new CompilationInput(
            "System.Text.Json.JsonSerializer.Serialize(42)",
            Language.CSharp,
            ExtraReferences: ["System.Text.Json"]
        );

        var result = _compilation.Compile(input);

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.AssemblyBytes);
    }

    [TestMethod]
    public void Compile_WithExtraImport_UsesNamespace()
    {
        var input = new CompilationInput(
            "JsonSerializer.Serialize(42)",
            Language.CSharp,
            ExtraReferences: ["System.Text.Json"],
            ExtraImports: ["System.Text.Json"]
        );

        var result = _compilation.Compile(input);

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.AssemblyBytes);
    }

    [TestMethod]
    public void Compile_WithoutExtraReference_FailsForUnreferencedType()
    {
        var input = new CompilationInput(
            "System.Text.Json.JsonSerializer.Serialize(42)",
            Language.CSharp
        );

        var result = _compilation.Compile(input);

        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.Diagnostics.Any(d => d.Severity == "Error"));
    }

    [TestMethod]
    public void Compile_WithExtraImport_LineOffsetCorrect()
    {
        var input = new CompilationInput(
            "invalid_code_here",
            Language.CSharp,
            ExtraImports: ["System.Text.Json", "System.Net.Http"]
        );

        var result = _compilation.Compile(input);

        Assert.IsFalse(result.Success);
        var error = result.Diagnostics.First(d => d.Severity == "Error");
        Assert.AreEqual(0, error.Line,
            $"Expected line 0 (user's first line), got {error.Line} with message: {error.Message}");
    }

    [TestMethod]
    public void Compile_ExtraImport_InjectedIntoGeneratedCode()
    {
        var engine = new CSharpTemplateEngine();
        var code = engine.GenerateCode("2 + 2", ["System.Text.Json"]);

        Assert.Contains("using System.Text.Json;", code);
        Assert.Contains("using System;", code);
    }

    [TestMethod]
    public void Compile_VbNet_ExtraImport_InjectedIntoGeneratedCode()
    {
        var engine = new VbTemplateEngine();
        var code = engine.GenerateCode("2 + 2", ["System.Text.Json"]);

        Assert.Contains("Imports System.Text.Json", code);
        Assert.Contains("Imports System", code);
    }

    [TestMethod]
    public void Compile_WithoutExtraImports_DoesNotContainExtraUsings()
    {
        var engine = new CSharpTemplateEngine();
        var code = engine.GenerateCode("2 + 2");

        Assert.Contains("using System;", code);
        Assert.Contains("using System.Threading.Tasks;", code);
    }

    [TestMethod]
    public void Compile_MultipleExtraImports_AllInjected()
    {
        var engine = new CSharpTemplateEngine();
        var code = engine.GenerateCode("2 + 2", ["System.Text.Json", "System.Net.Http"]);

        Assert.Contains("using System.Text.Json;", code);
        Assert.Contains("using System.Net.Http;", code);
    }
}
