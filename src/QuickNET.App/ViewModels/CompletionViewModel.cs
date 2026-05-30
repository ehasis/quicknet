using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using QuickNET.Models;

namespace QuickNET.App.ViewModels;

public partial class CompletionViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<CompletionItemViewModel> _items = [];

    [ObservableProperty]
    private CompletionItemViewModel? _selectedItem;

    [ObservableProperty]
    private bool _isVisible;

    public void SetItems(IEnumerable<CompletionItem> items)
    {
        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(new CompletionItemViewModel(item));
        }
        SelectedItem = Items.FirstOrDefault();
        IsVisible = Items.Count > 0;
    }

    public void Hide()
    {
        IsVisible = false;
        Items.Clear();
        SelectedItem = null;
    }
}

public class CompletionItemViewModel
{
    public string DisplayText { get; }
    public string InsertText { get; }
    public string? Description { get; }
    public string KindIcon { get; }

    public CompletionItemViewModel(CompletionItem item)
    {
        DisplayText = item.DisplayText;
        InsertText = item.InsertText;
        Description = item.Description;
        KindIcon = item.Kind switch
        {
            CompletionItemKind.Keyword => "K",
            CompletionItemKind.Method => "M",
            CompletionItemKind.Property => "P",
            CompletionItemKind.Field => "F",
            CompletionItemKind.Class => "C",
            CompletionItemKind.Struct => "S",
            CompletionItemKind.Interface => "I",
            CompletionItemKind.Enum => "E",
            CompletionItemKind.Namespace => "N",
            _ => "?"
        };
    }
}
