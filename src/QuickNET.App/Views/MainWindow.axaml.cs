using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Threading;
using QuickNET.App.Models;
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
            vm.ConversationItems.CollectionChanged += (_, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Reset)
                {
                    ConversationOutput.Inlines?.Clear();
                    return;
                }
                if (args.NewItems is not null)
                {
                    foreach (ConversationItem item in args.NewItems)
                        AppendItem(item);
                }
                ConversationScroller.ScrollToEnd();
            };

            foreach (var item in vm.ConversationItems)
                AppendItem(item);

            Dispatcher.UIThread.InvokeAsync(ConversationScroller.ScrollToEnd,
                DispatcherPriority.Background);
            Dispatcher.UIThread.InvokeAsync(() => InputBox.Focus(),
                DispatcherPriority.Background);
        }
    }

    private void AppendItem(ConversationItem item)
    {
        ConversationOutput.Inlines!.Add(new Run
        {
            Text = item.DisplayText,
            Foreground = item.Foreground
        });
        ConversationOutput.Inlines.Add(new LineBreak());
    }
}
