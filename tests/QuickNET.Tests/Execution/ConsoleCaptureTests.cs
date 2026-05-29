using Microsoft.Extensions.DependencyInjection;
using QuickNET.Models;

namespace QuickNET.Tests.Execution;

[TestClass]
public class ConsoleCaptureTests
{
    [TestMethod]
    public void ConsoleRedirect_WorksDirectly()
    {
        var originalOut = Console.Out;
        Console.WriteLine($"originalOut type: {originalOut.GetType().Name}");

        var sw = new StringWriter();
        Console.SetOut(sw);
        Console.WriteLine("test123");

        var currentOut = Console.Out;
        Console.SetOut(originalOut);

        Console.WriteLine($"currentOut is sw: {ReferenceEquals(currentOut, sw)}");
        var captured = sw.ToString();
        Console.WriteLine($"Captured: [{captured.Trim()}]");
        Assert.IsTrue(captured.Contains("test123"));
    }

    [TestMethod]
    public void ConsoleRedirect_WorksFromLoadedAssembly()
    {
        var services = new ServiceCollection();
        services.AddQuickNETCore();
        var provider = services.BuildServiceProvider();
        var engine = provider.GetRequiredService<ReplEngine>();

        var result = engine.Execute("System.Console.WriteLine(\"hello\"); return 42;", Language.CSharp);

        Assert.IsTrue(result.Success);
        Assert.AreEqual("42", result.Output);
        Assert.IsNotNull(result.ConsoleOutput);
        Assert.IsTrue(result.ConsoleOutput.Contains("hello"),
            $"ConsoleOutput was: [{result.ConsoleOutput}]");
    }
}

