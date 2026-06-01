using QuickNET.App.ViewModels;
using QuickNET.Models;

namespace QuickNET.Tests.ViewModels;

[TestClass]
public sealed class SignatureHelpViewModelTests
{
    [TestMethod]
    public void Show_WithSegments_SetsIsVisible()
    {
        var vm = new SignatureHelpViewModel();
        var segments = new List<SignatureHelpSegment>
        {
            new("int Math.Max(", false),
            new("int val1", true),
            new(", ", false),
            new("int val2", false),
            new(")", false)
        };

        vm.Show(segments);

        Assert.IsTrue(vm.IsVisible);
    }

    [TestMethod]
    public void Show_WithSegments_SetsSignatureText()
    {
        var vm = new SignatureHelpViewModel();
        var segments = new List<SignatureHelpSegment>
        {
            new("void Foo(", false),
            new("int x", true),
            new(")", false)
        };

        vm.Show(segments);

        Assert.IsTrue(vm.SignatureText.Contains("void Foo("));
        Assert.IsTrue(vm.SignatureText.Contains("int x"));
        Assert.IsTrue(vm.SignatureText.Contains(")"));
    }

    [TestMethod]
    public void Show_WithActiveParam_SetsBounds()
    {
        var vm = new SignatureHelpViewModel();
        var segments = new List<SignatureHelpSegment>
        {
            new("int Math.Max(", false),
            new("int val1", true),
            new(", ", false),
            new("int val2", false),
            new(")", false)
        };

        vm.Show(segments);

        Assert.IsTrue(vm.ActiveParameterStart >= 0);
        Assert.IsTrue(vm.ActiveParameterLength > 0);
    }

    [TestMethod]
    public void Hide_ClearsState()
    {
        var vm = new SignatureHelpViewModel();
        vm.Show([new SignatureHelpSegment("test", false)]);
        Assert.IsTrue(vm.IsVisible);

        vm.Hide();

        Assert.IsFalse(vm.IsVisible);
        Assert.AreEqual("", vm.SignatureText);
        Assert.AreEqual(-1, vm.ActiveParameterStart);
        Assert.AreEqual(0, vm.ActiveParameterLength);
    }

    [TestMethod]
    public void Show_WithoutActiveParam_HasNegativeStart()
    {
        var vm = new SignatureHelpViewModel();
        var segments = new List<SignatureHelpSegment>
        {
            new("void Bar()", false)
        };

        vm.Show(segments);

        Assert.AreEqual(-1, vm.ActiveParameterStart);
    }

    [TestMethod]
    public void Show_EmptySegments_SetsEmptyText()
    {
        var vm = new SignatureHelpViewModel();
        vm.Show([]);

        Assert.AreEqual("", vm.SignatureText);
        Assert.IsFalse(vm.IsVisible);
    }
}
