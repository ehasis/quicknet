using System.Collections.ObjectModel;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickNET.App.Completion;
using QuickNET.App.Models;
using QuickNET.Completion;
using QuickNET.History;
using QuickNET.MetaCommands;
using QuickNET.Models;
using QuickNET.Session;
using QuickNET.Theme;

namespace QuickNET.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ReplEngine _engine;
    private readonly HistoryService _history;
    private readonly MetaCommandService _metaCommandService;
    private readonly SessionState _sessionState;
    private readonly ThemeService _themeService;
    private readonly CompletionEngine _completionEngine;
    private readonly InputHistoryService _inputHistory;
    private CancellationTokenSource? _completionCts;
    private DispatcherTimer? _completionDebounceTimer;
    private int _inputBoxCaretPosition;

    public event EventHandler? CloseRequested;
    public event EventHandler? CompletionRequested;
    public event EventHandler<int>? CaretPositionChanged;
    public event EventHandler<int>? HistoryCursorMoved;

    [ObservableProperty]
    private string _inputText = "";

    [ObservableProperty]
    private int _selectedLanguageIndex = 0;

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<ConversationItem> ConversationItems { get; } = [];

    public CompletionViewModel Completion { get; } = new();

    public MainWindowViewModel(ReplEngine engine, HistoryService history,
        MetaCommandService metaCommandService, SessionState sessionState,
        ThemeService themeService, CompletionEngine completionEngine,
        InputHistoryService inputHistory)
    {
        _engine = engine;
        _history = history;
        _metaCommandService = metaCommandService;
        _sessionState = sessionState;
        _themeService = themeService;
        _completionEngine = completionEngine;
        _inputHistory = inputHistory;
        LoadHistory();

        _selectedLanguageIndex = _sessionState.CurrentLanguage == Language.CSharp ? 0 : 1;
    }

    private void LoadHistory()
    {
        foreach (var entry in _history.GetEntries())
        {
            ConversationItems.Add(new ConversationItem
            {
                DisplayText = $"> {entry.Input}",
                IsInput = true
            });
            ConversationItems.Add(new ConversationItem
            {
                DisplayText = entry.Output,
                IsInput = false,
                IsError = entry.IsError
            });
        }
    }

    [RelayCommand]
    private void ExecuteCode()
    {
        var input = InputText.TrimEnd('\r', '\n', ' ');
        if (string.IsNullOrWhiteSpace(input)) return;

        if (MetaCommandParser.IsMetaCommand(input))
        {
            ExecuteMetaCommand(input);
            InputText = "";
            return;
        }

        var language = SelectedLanguageIndex == 0 ? Language.CSharp : Language.VisualBasic;
        var langLabel = SelectedLanguageIndex == 0 ? "CSharp" : "VisualBasic";

        StatusText = $"Running ({langLabel})...";

        ConversationItems.Add(new ConversationItem
        {
            DisplayText = $"> {input}",
            IsInput = true
        });

        var result = _engine.Execute(input, language);

        string outputText;
        if (result.Success)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(result.ConsoleOutput))
                parts.Add(result.ConsoleOutput.TrimEnd());
            if (!string.IsNullOrEmpty(result.Output))
                parts.Add(result.Output);
            outputText = parts.Count > 0 ? string.Join("\n", parts) : "(no output)";
        }
        else
        {
            outputText = result.Error ?? result.Output ?? "Unknown error";
        }

        ConversationItems.Add(new ConversationItem
        {
            DisplayText = outputText,
            IsInput = false,
            IsError = !result.Success
        });

        _history.Record(input, langLabel, outputText, !result.Success);
        _inputHistory.Record(input);

        InputText = "";
        StatusText = result.Success ? "Ready" : "Error";
    }

    private void ExecuteMetaCommand(string input)
    {
        var result = _metaCommandService.Execute(input);

        ConversationItems.Add(new ConversationItem
        {
            DisplayText = $"> {input.TrimEnd()}",
            IsInput = true
        });

        if (result.Command == "exit")
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        if (result.Command == "clear")
        {
            ConversationItems.Clear();
            _history.Clear();
        }

        if (result.Command == "lang" && result.Success)
        {
            SelectedLanguageIndex = _sessionState.CurrentLanguage == Language.CSharp ? 0 : 1;
        }

        OnPropertyChanged(nameof(SessionInfoText));

        ConversationItems.Add(new ConversationItem
        {
            DisplayText = result.DisplayText,
            IsInput = false,
            IsError = !result.Success
        });

        StatusText = result.Success ? "Ready" : "Error";
    }

    [RelayCommand]
    private void ClearHistory()
    {
        ConversationItems.Clear();
        _history.Clear();
        StatusText = "History cleared";
    }

    public string SessionInfoText
    {
        get
        {
            var lang = _sessionState.CurrentLanguage == Language.CSharp ? "C#" : "VB";
            var timeoutLabel = _sessionState.TimeoutSeconds == 0 ? "No Limit" : $"{_sessionState.TimeoutSeconds}s";
            var themeLabel = _themeService.CurrentTheme switch
            {
                AppTheme.Light => "Light",
                AppTheme.Dark => "Dark",
                _ => ""
            };
            var themePart = string.IsNullOrEmpty(themeLabel) ? "" : $"{themeLabel} | ";
            return $"{themePart}{lang} | Timeout: {timeoutLabel} | Refs: {_sessionState.ExtraReferences.Count} | Imports: {_sessionState.ExtraImports.Count}";
        }
    }

    public void OnCaretPositionChanged(int position)
    {
        _inputBoxCaretPosition = position;
    }

    public void RequestCompletions(string code, int cursorPosition)
    {
        _completionCts?.Cancel();
        _completionCts?.Dispose();
        _completionCts = null;

        _completionDebounceTimer?.Stop();
        _completionDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _completionDebounceTimer.Tick += async (_, _) =>
        {
            _completionDebounceTimer.Stop();
            await FetchCompletions(code, cursorPosition);
        };
        _completionDebounceTimer.Start();
    }

    public void RequestCompletionsManually()
    {
        Completion.Hide();
        _completionCts?.Cancel();
        _completionCts?.Dispose();
        _completionCts = null;
        _completionDebounceTimer?.Stop();
        _ = FetchCompletions(InputText, _inputBoxCaretPosition);
    }

    private async Task FetchCompletions(string code, int cursorPosition)
    {
        _completionCts = new CancellationTokenSource();
        var ct = _completionCts.Token;

        try
        {
            var language = SelectedLanguageIndex == 0 ? Language.CSharp : Language.VisualBasic;
            var items = await _completionEngine.GetCompletionsAsync(
                code, cursorPosition, language,
                _sessionState.ExtraReferences, _sessionState.ExtraImports, ct);

            if (!ct.IsCancellationRequested)
            {
                Completion.SetItems(items);
                CompletionRequested?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { Completion.Hide(); }
    }

    public void AcceptCompletion()
    {
        if (!Completion.IsVisible || Completion.SelectedItem is null) return;

        var insertText = Completion.SelectedItem.InsertText;
        var cursorPos = _inputBoxCaretPosition;

        var wordStart = cursorPos;
        while (wordStart > 0 && (char.IsLetterOrDigit(InputText[wordStart - 1]) || InputText[wordStart - 1] == '_'))
            wordStart--;

        var before = InputText[..wordStart];
        var after = InputText[cursorPos..];
        InputText = before + insertText + after;

        Completion.Hide();
        CaretPositionChanged?.Invoke(this, wordStart + insertText.Length);
    }

    public void NavigateHistoryOlder()
    {
        var current = InputText ?? "";
        var result = _inputHistory.NavigateOlder(current);
        if (result is not null)
        {
            InputText = result;
            HistoryCursorMoved?.Invoke(this, result.Length);
        }
    }

    public void NavigateHistoryNewer()
    {
        var result = _inputHistory.NavigateNewer();
        if (result is not null)
        {
            InputText = result;
            HistoryCursorMoved?.Invoke(this, result.Length);
        }
    }

    public void ResetHistoryNavigation()
    {
        _inputHistory.Reset();
    }
}
