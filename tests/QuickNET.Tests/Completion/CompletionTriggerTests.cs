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
}
