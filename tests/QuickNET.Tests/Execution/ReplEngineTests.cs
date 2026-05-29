using Microsoft.Extensions.DependencyInjection;
using QuickNET.Models;

namespace QuickNET.Tests.Execution;

[TestClass]
public class ReplEngineTests
{
    private readonly ReplEngine _engine;

    public ReplEngineTests()
    {
        var services = new ServiceCollection();
        services.AddQuickNETCore();
        var provider = services.BuildServiceProvider();
        _engine = provider.GetRequiredService<ReplEngine>();
    }

    [TestMethod]
    public void Execute_CSharp_SimpleExpression_ReturnsOutput()
    {
        var result = _engine.Execute("2 + 2", Language.CSharp);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("4", result.Output);
        Assert.IsNull(result.Error);
    }

    [TestMethod]
    public void Execute_CSharp_ConsoleWriteLine_CapturesOutput()
    {
        var result = _engine.Execute("Console.WriteLine(\"hello\");", Language.CSharp);

        if (!result.Success)
        {
            Assert.Fail($"Compilation failed: {result.Error}");
        }

        Assert.IsNotNull(result.ConsoleOutput);
        StringAssert.Contains(result.ConsoleOutput, "hello");
    }

    [TestMethod]
    public void Execute_CSharp_ThrowException_ReturnsError()
    {
        var result = _engine.Execute("throw new Exception(\"fail\");", Language.CSharp);

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        StringAssert.Contains(result.Error, "fail");
    }

    [TestMethod]
    public void Execute_CSharp_TaskFromResult_UnwrapsTask()
    {
        var result = _engine.Execute("System.Threading.Tasks.Task.FromResult(42)", Language.CSharp);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("42", result.Output);
    }

    [TestMethod]
    public void Execute_VisualBasic_SimpleExpression_ReturnsOutput()
    {
        var result = _engine.Execute("2 + 2", Language.VisualBasic);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("4", result.Output);
    }

    [TestMethod]
    public void Execute_MultipleInvocations_DoesNotLeakMemory()
    {
        long memoryBefore = GC.GetTotalMemory(forceFullCollection: true);

        for (int i = 0; i < 10; i++)
        {
            var result = _engine.Execute("2 + 2", Language.CSharp);
            Assert.IsTrue(result.Success);
        }

        long memoryAfter = GC.GetTotalMemory(forceFullCollection: true);
        long growth = memoryAfter - memoryBefore;

        Assert.IsTrue(growth < 20 * 1024 * 1024,
            $"Memory grew by {growth / 1024}KB, expected less than 20MB");
    }

    [TestMethod]
    public void Execute_CSharp_InvalidCode_ReturnsCompilationError()
    {
        var result = _engine.Execute("2 +", Language.CSharp);

        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.Error);
        StringAssert.Contains(result.Error, "Error");
    }

    [TestMethod]
    public void Execute_CSharp_ReturnValue_ReturnsCorrectOutput()
    {
        var result = _engine.Execute("return 100;", Language.CSharp);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("100", result.Output);
    }
}
