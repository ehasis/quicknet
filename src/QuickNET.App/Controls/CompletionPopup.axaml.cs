using Avalonia.Controls;

namespace QuickNET.App.Controls;

public partial class CompletionPopup : UserControl
{
    public CompletionPopup()
    {
        InitializeComponent();
    }

    public void MoveSelection(int delta)
    {
        var list = CompletionList;
        if (list.ItemCount == 0) return;

        var newIndex = (list.SelectedIndex + delta + list.ItemCount) % list.ItemCount;
        list.SelectedIndex = newIndex;
        list.ScrollIntoView(newIndex);
    }

    public void MoveSelectionByPage(int delta)
    {
        var list = CompletionList;
        if (list.ItemCount == 0) return;

        var step = Math.Max(1, (int)(list.Bounds.Height / 40));
        var newIndex = list.SelectedIndex + delta * step;
        if (newIndex < 0) newIndex = 0;
        if (newIndex >= list.ItemCount) newIndex = list.ItemCount - 1;
        list.SelectedIndex = newIndex;
        list.ScrollIntoView(newIndex);
    }
}
