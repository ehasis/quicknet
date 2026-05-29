using Avalonia.Controls;
using Avalonia.Input;
using QuickNET.App.ViewModels;

namespace QuickNET.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: true);
        DataContextChanged += OnDataContextChanged;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (e.KeyModifiers == KeyModifiers.None)
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.ExecuteCodeCommand.Execute(null);
                }
                e.Handled = true;
            }
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ConversationItems.CollectionChanged += (_, _) =>
            {
                ConversationScroller.ScrollToEnd();
            };
        }
    }
}
