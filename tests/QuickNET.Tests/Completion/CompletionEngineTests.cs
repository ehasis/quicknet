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
    public async Task GetCompletions_DotTrigger_IsNotEmpty()
    {
        var engine = CreateEngine();
        var items = await engine.GetCompletionsAsync("System.", 7, Language.CSharp);

        Assert.IsNotEmpty(items);
    }

    [TestMethod]
    public async Task GetCompletions_IdentifierPrefix_IsNotEmpty()
    {
        var engine = CreateEngine();
        var items = await engine.GetCompletionsAsync("Sys", 3, Language.CSharp);

        Assert.IsNotEmpty(items);
    }

    [TestMethod]
    public async Task GetCompletions_EmptyInput_IsNotEmpty()
    {
        var engine = CreateEngine();
        var items = await engine.GetCompletionsAsync("", 0, Language.CSharp);

        Assert.IsNotEmpty(items);
    }

    [TestMethod]
    public async Task GetCompletions_CancellationToken_CancelsOperation()
    {
        var engine = CreateEngine();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            await engine.GetCompletionsAsync("System.", 7, Language.CSharp,
                ct: cts.Token);
            Assert.Fail("Expected OperationCanceledException");
        }
        catch (OperationCanceledException)
        {
        }
    }

    [TestMethod]
    public async Task GetCompletions_CSharp_Language_IsNotEmpty()
    {
        var engine = CreateEngine();
        var items = await engine.GetCompletionsAsync("System.", 7, Language.CSharp);

        Assert.IsNotEmpty(items);
    }

    [TestMethod]
    public async Task GetCompletions_VisualBasic_Language_IsNotEmpty()
    {
        var engine = CreateEngine();
        var items = await engine.GetCompletionsAsync("System.", 7, Language.VisualBasic);

        Assert.IsNotEmpty(items);
    }

    [TestMethod]
    public async Task GetCompletions_LanguageSwitch_IsNotEmpty()
    {
        var engine = CreateEngine();

        var csharpItems = await engine.GetCompletionsAsync("System.", 7, Language.CSharp);
        Assert.IsNotEmpty(csharpItems);

        var vbItems = await engine.GetCompletionsAsync("System.", 7, Language.VisualBasic);
        Assert.IsNotEmpty(vbItems);

        var csharpAgain = await engine.GetCompletionsAsync("System.", 7, Language.CSharp);
        Assert.IsNotEmpty(csharpAgain);
    }

    [TestMethod]
    public async Task GetCompletions_ExtraReferences_IsNotEmpty()
    {
        var engine = CreateEngine();

        var first = await engine.GetCompletionsAsync("System.", 7, Language.CSharp);
        Assert.IsNotEmpty(first);

        var second = await engine.GetCompletionsAsync("System.", 7, Language.CSharp,
            extraReferences: ["System.Text.Json"]);
        Assert.IsNotEmpty(second);

        var third = await engine.GetCompletionsAsync("System.", 7, Language.CSharp);
        Assert.IsNotEmpty(third);
    }

    [TestMethod]
    public async Task GetCompletions_CursorPositionAtZero_IsNotEmpty()
    {
        var engine = CreateEngine();
        var items = await engine.GetCompletionsAsync("System", 0, Language.CSharp);

        Assert.IsNotEmpty(items);
    }
}
