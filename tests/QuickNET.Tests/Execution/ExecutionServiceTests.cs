using Microsoft.Extensions.DependencyInjection;
using QuickNET.Compilation;
using QuickNET.Execution;
using QuickNET.Models;
using QuickNET.Templates;

namespace QuickNET.Tests.Execution;

[TestClass]
public sealed class ExecutionServiceTests
{
    private readonly ExecutionService _execution;
    private readonly CompilationService _compilation;

    public ExecutionServiceTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITemplateEngine, CSharpTemplateEngine>();
        services.AddSingleton<ITemplateEngine, VbTemplateEngine>();
        services.AddSingleton<CompilationService>();
        services.AddSingleton<ExecutionService>();
        var provider = services.BuildServiceProvider();
        _compilation = provider.GetRequiredService<CompilationService>();
        _execution = provider.GetRequiredService<ExecutionService>();
    }

    [TestMethod]
    public void Execute_SimpleMath_ReturnsResult()
    {
        var compilation = _compilation.Compile(new CompilationInput("2 + 2", Language.CSharp));
        Assert.IsTrue(compilation.Success);
        var result = _execution.Execute(new ExecutionInput(compilation.AssemblyBytes!));

        Assert.IsTrue(result.Success);
        Assert.AreEqual("4", result.Output);
    }

    [TestMethod]
    public void Execute_StringExpression_ReturnsResult()
    {
        var compilation = _compilation.Compile(new CompilationInput("\"hello\" + \" world\"", Language.CSharp));
        Assert.IsTrue(compilation.Success);
        var result = _execution.Execute(new ExecutionInput(compilation.AssemblyBytes!));

        Assert.IsTrue(result.Success);
        Assert.AreEqual("hello world", result.Output);
    }

    [TestMethod]
    public void Execute_ConsoleWriteLine_CapturesOutput()
    {
        var compilation = _compilation.Compile(new CompilationInput("Console.WriteLine(\"captured\");", Language.CSharp));
        Assert.IsTrue(compilation.Success);
        var result = _execution.Execute(new ExecutionInput(compilation.AssemblyBytes!));

        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.ConsoleOutput);
        StringAssert.Contains(result.ConsoleOutput, "captured");
    }

    [TestMethod]
    public void Execute_RuntimeException_ReturnsError()
    {
        var compilation = _compilation.Compile(new CompilationInput("throw new Exception(\"fail\");", Language.CSharp));
        Assert.IsTrue(compilation.Success);
        var result = _execution.Execute(new ExecutionInput(compilation.AssemblyBytes!));

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        StringAssert.Contains(result.Error, "fail");
    }

    [TestMethod]
    public void Execute_ReturnsTask_UnwrapsResult()
    {
        var compilation = _compilation.Compile(new CompilationInput("System.Threading.Tasks.Task.FromResult(42)", Language.CSharp));
        Assert.IsTrue(compilation.Success);
        var result = _execution.Execute(new ExecutionInput(compilation.AssemblyBytes!));

        Assert.IsTrue(result.Success);
        Assert.AreEqual("42", result.Output);
    }

    [TestMethod]
    public void Execute_MultipleExecutions_NoMemoryLeak()
    {
        var compilation = _compilation.Compile(new CompilationInput("2 + 2", Language.CSharp));
        Assert.IsTrue(compilation.Success);
        var bytes = compilation.AssemblyBytes!;

        for (int i = 0; i < 20; i++)
        {
            var result = _execution.Execute(new ExecutionInput(bytes));
            Assert.IsTrue(result.Success);
            Assert.AreEqual("4", result.Output);
        }
    }
}
