using Microsoft.Extensions.DependencyInjection;
using QuickNET.Completion;
using QuickNET.Models;

namespace QuickNET.Tests.Completion;

[TestClass]
public sealed class CompletionEngineTests
{
    private static CompletionEngine CreateEngine()
    {
        var services = new ServiceCollection();
        services.AddQuickNETCore();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<CompletionEngine>();
    }

    [TestMethod]
    public async Task GetCompletions_DotTrigger_DoesNotThrow()
    {
        var engine = CreateEngine();
        var items = await engine.GetCompletionsAsync("Console.", 8, Language.CSharp);

        Assert.IsNotNull(items);
        // Completions may be empty in test environment due to MEF workspace limitations
    }

    [TestMethod]
    public async Task GetCompletions_IdentifierPrefix_DoesNotThrow()
    {
        var engine = CreateEngine();
        var items = await engine.GetCompletionsAsync("Con", 3, Language.CSharp);

        Assert.IsNotNull(items);
    }

    [TestMethod]
    public async Task GetCompletions_EmptyInput_DoesNotThrow()
    {
        var engine = CreateEngine();
        var items = await engine.GetCompletionsAsync("", 0, Language.CSharp);

        Assert.IsNotNull(items);
    }

    [TestMethod]
    public async Task GetCompletions_CancellationToken_CancelsOperation()
    {
        var engine = CreateEngine();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            await engine.GetCompletionsAsync("Console.", 8, Language.CSharp,
                ct: cts.Token);
            Assert.Fail("Expected OperationCanceledException");
        }
        catch (OperationCanceledException)
        {
        }
    }

    [TestMethod]
    public async Task GetCompletions_CSharp_Language_DoesNotThrow()
    {
        var engine = CreateEngine();
        var items = await engine.GetCompletionsAsync("Console.", 8, Language.CSharp);

        Assert.IsNotNull(items);
    }

    [TestMethod]
    public async Task GetCompletions_VisualBasic_Language_DoesNotThrow()
    {
        var engine = CreateEngine();
        var items = await engine.GetCompletionsAsync("Console.", 8, Language.VisualBasic);

        Assert.IsNotNull(items);
    }

    [TestMethod]
    public async Task GetCompletions_LanguageSwitch_DoesNotThrow()
    {
        var engine = CreateEngine();

        var csharpItems = await engine.GetCompletionsAsync("Console.", 8, Language.CSharp);
        Assert.IsNotNull(csharpItems);

        var vbItems = await engine.GetCompletionsAsync("Console.", 8, Language.VisualBasic);
        Assert.IsNotNull(vbItems);

        var csharpAgain = await engine.GetCompletionsAsync("Console.", 8, Language.CSharp);
        Assert.IsNotNull(csharpAgain);
    }

    [TestMethod]
    public async Task GetCompletions_ExtraReferences_DoesNotThrow()
    {
        var engine = CreateEngine();

        var first = await engine.GetCompletionsAsync("Console.", 8, Language.CSharp);
        Assert.IsNotNull(first);

        var second = await engine.GetCompletionsAsync("Console.", 8, Language.CSharp,
            extraReferences: ["System.Text.Json"]);
        Assert.IsNotNull(second);

        var third = await engine.GetCompletionsAsync("Console.", 8, Language.CSharp);
        Assert.IsNotNull(third);
    }

    [TestMethod]
    public async Task GetCompletions_CursorPositionAtZero_DoesNotThrow()
    {
        var engine = CreateEngine();
        var items = await engine.GetCompletionsAsync("Console", 0, Language.CSharp);

        Assert.IsNotNull(items);
    }
}
