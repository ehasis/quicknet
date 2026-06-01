using QuickNET.App.Completion;

namespace QuickNET.Tests.Completion;

[TestClass]
public sealed class CompletionTriggerTests
{
    [TestMethod]
    public void ShouldAutoTrigger_Dot_ReturnsTrue()
    {
        Assert.IsTrue(TriggerHelper.ShouldAutoTrigger("Console.", 8));
    }

    [TestMethod]
    public void ShouldAutoTrigger_NoDot_ReturnsFalse()
    {
        Assert.IsFalse(TriggerHelper.ShouldAutoTrigger("Console", 7));
    }

    [TestMethod]
    public void ShouldAutoTrigger_EmptyInput_ReturnsFalse()
    {
        Assert.IsFalse(TriggerHelper.ShouldAutoTrigger("", 0));
    }

    [TestMethod]
    public void ShouldAutoTrigger_PositionZero_ReturnsFalse()
    {
        Assert.IsFalse(TriggerHelper.ShouldAutoTrigger("abc", 0));
    }

    [TestMethod]
    public void ShouldAutoTrigger_PositionBeyondLength_ReturnsFalse()
    {
        Assert.IsFalse(TriggerHelper.ShouldAutoTrigger("abc", 5));
    }

    [TestMethod]
    public void ShouldAutoTrigger_DotNotAtCaret_ReturnsTrue()
    {
        Assert.IsTrue(TriggerHelper.ShouldAutoTrigger("obj.property", 4));
    }

    [TestMethod]
    public void ShouldTriggerSignatureHelp_OpenParen_ReturnsTrue()
    {
        Assert.IsTrue(TriggerHelper.ShouldTriggerSignatureHelp("Foo(", 4));
    }

    [TestMethod]
    public void ShouldTriggerSignatureHelp_Comma_ReturnsTrue()
    {
        Assert.IsTrue(TriggerHelper.ShouldTriggerSignatureHelp("Foo(1,", 6));
    }

    [TestMethod]
    public void ShouldTriggerSignatureHelp_OtherChar_ReturnsFalse()
    {
        Assert.IsFalse(TriggerHelper.ShouldTriggerSignatureHelp("Foo.", 4));
    }

    [TestMethod]
    public void ShouldTriggerSignatureHelp_EmptyString_ReturnsFalse()
    {
        Assert.IsFalse(TriggerHelper.ShouldTriggerSignatureHelp("", 0));
    }

    [TestMethod]
    public void ShouldTriggerSignatureHelp_PositionZero_ReturnsFalse()
    {
        Assert.IsFalse(TriggerHelper.ShouldTriggerSignatureHelp("(", 0));
    }
}
