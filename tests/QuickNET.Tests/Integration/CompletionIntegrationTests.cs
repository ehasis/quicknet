using Microsoft.Extensions.DependencyInjection;
using QuickNET.Completion;
using QuickNET.Models;

namespace QuickNET.Tests.Integration;

[TestClass]
public sealed class CompletionIntegrationTests
{
    private static CompletionEngine CreateEngine()
    {
        var services = new ServiceCollection();
        services.AddQuickNETCore();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<CompletionEngine>();
    }

    [TestMethod]
    public async Task Completion_DotTrigger_DoesNotThrow()
    {
        var engine = CreateEngine();
        var items = await engine.GetCompletionsAsync("Console.", 8, Language.CSharp);
        Assert.IsNotNull(items);
    }

    [TestMethod]
    public async Task Completion_RespectsLanguage_VB()
    {
        var engine = CreateEngine();
        var items = await engine.GetCompletionsAsync("Console.", 8, Language.VisualBasic);
        Assert.IsNotNull(items);
    }

    [TestMethod]
    public async Task Completion_WithExtraImport_DoesNotThrow()
    {
        var engine = CreateEngine();
        var items = await engine.GetCompletionsAsync("JsonSerializer.", 14, Language.CSharp,
            extraReferences: ["System.Text.Json"],
            extraImports: ["System.Text.Json"]);
        Assert.IsNotNull(items);
    }

    [TestMethod]
    public async Task Completion_Cancelled_ThrowsOperationCanceledException()
    {
        var engine = CreateEngine();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            await engine.GetCompletionsAsync("Console.", 8, Language.CSharp, ct: cts.Token);
            Assert.Fail("Expected OperationCanceledException");
        }
        catch (OperationCanceledException)
        {
        }
    }
}
