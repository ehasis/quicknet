using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using QuickNET.App.Completion;
using QuickNET.App.Models;
using QuickNET.App.ViewModels;

namespace QuickNET.App.Views;

public partial class MainWindow : Window
{
    private bool _isNavigatingHistory;
    private bool _isAcceptingCompletion;

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, handledEventsToo: true);
        AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        DataContextChanged += OnDataContextChanged;
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        if (vm.Completion.IsVisible)
        {
            if (e.Key == Key.Tab)
            {
                _isAcceptingCompletion = true;
                vm.AcceptCompletion();
                e.Handled = true;
            }
        }
        else
        {
            if (e.Key == Key.Up && InputBox.CaretIndex == (InputBox.Text?.Length ?? 0))
            {
                vm.NavigateHistoryOlder();
                _isNavigatingHistory = true;
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Down && InputBox.CaretIndex == (InputBox.Text?.Length ?? 0))
            {
                vm.NavigateHistoryNewer();
                _isNavigatingHistory = true;
                e.Handled = true;
                return;
            }
        }

    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

        if (e.Key == Key.Space && e.KeyModifiers == KeyModifiers.Control)
        {
            vm.RequestCompletionsManually();
            e.Handled = true;
            return;
        }

        if (vm.Completion.IsVisible)
        {
            switch (e.Key)
            {
                case Key.Down:
                    CompletionPopupControl.MoveSelection(1);
                    e.Handled = true;
                    return;
                case Key.Up:
                    CompletionPopupControl.MoveSelection(-1);
                    e.Handled = true;
                    return;
                case Key.PageDown:
                    CompletionPopupControl.MoveSelectionByPage(1);
                    e.Handled = true;
                    return;
                case Key.PageUp:
                    CompletionPopupControl.MoveSelectionByPage(-1);
                    e.Handled = true;
                    return;
                case Key.Enter:
                    if (e.KeyModifiers == KeyModifiers.None)
                    {
                        _isAcceptingCompletion = true;
                        vm.AcceptCompletion();
                        e.Handled = true;
                        return;
                    }
                    break;
                case Key.Tab:
                    _isAcceptingCompletion = true;
                    vm.AcceptCompletion();
                    e.Handled = true;
                    return;
                case Key.Escape:
                    vm.Completion.Hide();
                    e.Handled = true;
                    return;
            }
        }

        if (e.Key == Key.Enter)
        {
            if (e.KeyModifiers == KeyModifiers.None)
            {
                vm.ExecuteCodeCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not MainWindowViewModel vm) return;

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

        vm.CloseRequested += (_, _) => Close();

        vm.CompletionRequested += (_, _) => PositionCompletionPopup();

        vm.CaretPositionChanged += (_, position) =>
        {
            InputBox.CaretIndex = position;
        };

        vm.HistoryCursorMoved += (_, position) =>
        {
            InputBox.CaretIndex = position;
        };

        CompletionOverlay.PlacementTarget = InputBox;

        InputBox.TextChanged += (_, _) =>
        {
            var text = InputBox.Text ?? "";
            var pos = InputBox.CaretIndex;

            if (!_isNavigatingHistory && !_isAcceptingCompletion)
            {
                vm.ResetHistoryNavigation();
            }
            _isNavigatingHistory = false;
            _isAcceptingCompletion = false;

            if (vm.Completion.IsVisible)
            {
                vm.RequestCompletions(text, pos);
            }
            else if (TriggerHelper.ShouldAutoTrigger(text, pos))
            {
                vm.RequestCompletions(text, pos);
            }
            else
            {
                vm.Completion.Hide();
            }
        };

        InputBox.PropertyChanged += (s, args) =>
        {
            if (args.Property == TextBox.CaretIndexProperty)
            {
                vm.OnCaretPositionChanged(InputBox.CaretIndex);
            }
        };

        foreach (var item in vm.ConversationItems)
            AppendItem(item);

        Dispatcher.UIThread.InvokeAsync(ConversationScroller.ScrollToEnd,
            DispatcherPriority.Background);
        Dispatcher.UIThread.InvokeAsync(() => InputBox.Focus(),
            DispatcherPriority.Background);
    }

    private void PositionCompletionPopup()
    {
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
