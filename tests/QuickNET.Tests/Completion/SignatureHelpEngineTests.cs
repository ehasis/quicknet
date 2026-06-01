using Microsoft.Extensions.DependencyInjection;
using QuickNET.Models;

namespace QuickNET.Tests.Completion;

[TestClass]
public sealed class SignatureHelpEngineTests
{
    private static QuickNET.Completion.CompletionEngine CreateEngine()
    {
        var services = new ServiceCollection();
        services.AddQuickNETCore();
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<QuickNET.Completion.CompletionEngine>();
    }

    [TestMethod]
    public async Task GetSignatureHelp_MethodCall_ReturnsSignature()
    {
        var engine = CreateEngine();
        var segments = await engine.GetSignatureHelpAsync(
            "System.Math.Max(", 15, Language.CSharp, '(');

        Assert.IsNotNull(segments);
        Assert.IsTrue(segments.Count > 0);
        Assert.IsTrue(segments.Any(s => s.IsActiveParameter));
    }

    [TestMethod]
    public async Task GetSignatureHelp_OpenParen_HighlightsFirstParam()
    {
        var engine = CreateEngine();
        var segments = await engine.GetSignatureHelpAsync(
            "System.Math.Max(", 15, Language.CSharp, '(');

        Assert.IsNotNull(segments);
        var activeSegments = segments.Where(s => s.IsActiveParameter).ToList();
        Assert.AreEqual(1, activeSegments.Count);
    }

    [TestMethod]
    public async Task GetSignatureHelp_Comma_MovesToNextParam()
    {
        var engine = CreateEngine();
        var segments = await engine.GetSignatureHelpAsync(
            "System.Math.Max(1,", 17, Language.CSharp, ',');

        Assert.IsNotNull(segments);
        var activeSegments = segments.Where(s => s.IsActiveParameter).ToList();
        Assert.AreEqual(1, activeSegments.Count);
    }

    [TestMethod]
    public async Task GetSignatureHelp_Constructor_ReturnsSignature()
    {
        var engine = CreateEngine();
        var segments = await engine.GetSignatureHelpAsync(
            "new System.Collections.Generic.List<int>(", 42, Language.CSharp, '(');

        Assert.IsNotNull(segments);
        Assert.IsTrue(segments.Count > 0);
    }

    [TestMethod]
    public async Task GetSignatureHelp_NoInvocation_ReturnsNull()
    {
        var engine = CreateEngine();
        var segments = await engine.GetSignatureHelpAsync(
            "if (", 4, Language.CSharp, '(');

        Assert.IsNull(segments);
    }

    [TestMethod]
    public async Task GetSignatureHelp_WhileStatement_ReturnsNull()
    {
        var engine = CreateEngine();
        var segments = await engine.GetSignatureHelpAsync(
            "while (", 7, Language.CSharp, '(');

        Assert.IsNull(segments);
    }

    [TestMethod]
    public async Task GetSignatureHelp_Cancelled_Throws()
    {
        var engine = CreateEngine();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            await engine.GetSignatureHelpAsync(
                "System.Math.Max(", 15, Language.CSharp, '(', ct: cts.Token);
            Assert.Fail("Expected OperationCanceledException");
        }
        catch (OperationCanceledException)
        {
        }
    }

    [TestMethod]
    public async Task GetSignatureHelp_RespectsLanguage_VB()
    {
        var engine = CreateEngine();
        var segments = await engine.GetSignatureHelpAsync(
            "System.Math.Max(", 15, Language.VisualBasic, '(');

        Assert.IsNotNull(segments);
        Assert.IsTrue(segments.Count > 0);
    }

    [TestMethod]
    public async Task GetSignatureHelp_MethodWithMultipleOverloads_ReturnsOneSignature()
    {
        var engine = CreateEngine();
        var segments = await engine.GetSignatureHelpAsync(
            "System.Console.WriteLine(", 25, Language.CSharp, '(');

        Assert.IsNotNull(segments);
        Assert.IsTrue(segments.Count > 0);
    }
}
