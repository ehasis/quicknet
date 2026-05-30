using QuickNET.App.ViewModels;
using QuickNET.Models;

namespace QuickNET.Tests.ViewModels;

[TestClass]
public sealed class CompletionViewModelTests
{
    [TestMethod]
    public void SetItems_NonEmpty_SetsVisible()
    {
        var vm = new CompletionViewModel();
        var items = new List<CompletionItem>
        {
            new() { DisplayText = "item1", InsertText = "item1", Kind = CompletionItemKind.Keyword },
            new() { DisplayText = "item2", InsertText = "item2", Kind = CompletionItemKind.Method }
        };

        vm.SetItems(items);

        Assert.IsTrue(vm.IsVisible);
        Assert.AreEqual(2, vm.Items.Count);
    }

    [TestMethod]
    public void SetItems_Empty_HidesPopup()
    {
        var vm = new CompletionViewModel();

        vm.SetItems([]);

        Assert.IsFalse(vm.IsVisible);
    }

    [TestMethod]
    public void SetItems_NullOrEmpty_SetsFirstSelected()
    {
        var vm = new CompletionViewModel();
        var items = new List<CompletionItem>
        {
            new() { DisplayText = "first", InsertText = "first", Kind = CompletionItemKind.Keyword },
            new() { DisplayText = "second", InsertText = "second", Kind = CompletionItemKind.Method }
        };

        vm.SetItems(items);

        Assert.IsNotNull(vm.SelectedItem);
        Assert.AreEqual("first", vm.SelectedItem.DisplayText);
    }

    [TestMethod]
    public void Hide_ClearsItemsAndHides()
    {
        var vm = new CompletionViewModel();
        vm.SetItems([new CompletionItem { DisplayText = "x", InsertText = "x", Kind = CompletionItemKind.Keyword }]);
        Assert.IsTrue(vm.IsVisible);

        vm.Hide();

        Assert.IsFalse(vm.IsVisible);
        Assert.AreEqual(0, vm.Items.Count);
        Assert.IsNull(vm.SelectedItem);
    }

    [TestMethod]
    public void CompletionItemViewModel_Kinds_MapCorrectly()
    {
        Assert.AreEqual("K", new CompletionItemViewModel(
            new CompletionItem { DisplayText = "", InsertText = "", Kind = CompletionItemKind.Keyword }).KindIcon);
        Assert.AreEqual("M", new CompletionItemViewModel(
            new CompletionItem { DisplayText = "", InsertText = "", Kind = CompletionItemKind.Method }).KindIcon);
        Assert.AreEqual("P", new CompletionItemViewModel(
            new CompletionItem { DisplayText = "", InsertText = "", Kind = CompletionItemKind.Property }).KindIcon);
        Assert.AreEqual("F", new CompletionItemViewModel(
            new CompletionItem { DisplayText = "", InsertText = "", Kind = CompletionItemKind.Field }).KindIcon);
        Assert.AreEqual("C", new CompletionItemViewModel(
            new CompletionItem { DisplayText = "", InsertText = "", Kind = CompletionItemKind.Class }).KindIcon);
        Assert.AreEqual("S", new CompletionItemViewModel(
            new CompletionItem { DisplayText = "", InsertText = "", Kind = CompletionItemKind.Struct }).KindIcon);
        Assert.AreEqual("I", new CompletionItemViewModel(
            new CompletionItem { DisplayText = "", InsertText = "", Kind = CompletionItemKind.Interface }).KindIcon);
        Assert.AreEqual("E", new CompletionItemViewModel(
            new CompletionItem { DisplayText = "", InsertText = "", Kind = CompletionItemKind.Enum }).KindIcon);
        Assert.AreEqual("N", new CompletionItemViewModel(
            new CompletionItem { DisplayText = "", InsertText = "", Kind = CompletionItemKind.Namespace }).KindIcon);
        Assert.AreEqual("?", new CompletionItemViewModel(
            new CompletionItem { DisplayText = "", InsertText = "", Kind = CompletionItemKind.Unknown }).KindIcon);
    }
}
