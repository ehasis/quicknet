using Avalonia.Media;

namespace QuickNET.App.Models;

public class ConversationItem
{
    public string DisplayText { get; set; } = "";
    public bool IsInput { get; set; }
    public bool IsError { get; set; }
    public IBrush Foreground => IsInput ? Brushes.Teal : IsError ? Brushes.Tomato : Brushes.Gray;
}
