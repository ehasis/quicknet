using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickNET.App.Models;
using QuickNET.History;
using QuickNET.MetaCommands;
using QuickNET.Models;
using QuickNET.Session;

namespace QuickNET.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ReplEngine _engine;
    private readonly HistoryService _history;
    private readonly MetaCommandService _metaCommandService;
    private readonly SessionState _sessionState;

    private static readonly int[] TimeoutOptions = [5, 10, 30, 60, 0];

    [ObservableProperty]
    private string _inputText = "";

    [ObservableProperty]
    private int _selectedLanguageIndex = 0;

    [ObservableProperty]
    private int _selectedTimeoutIndex = 2;

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<ConversationItem> ConversationItems { get; } = [];

    public MainWindowViewModel(ReplEngine engine, HistoryService history,
        MetaCommandService metaCommandService, SessionState sessionState)
    {
        _engine = engine;
        _history = history;
        _metaCommandService = metaCommandService;
        _sessionState = sessionState;
        LoadHistory();
        RestoreSessionSettings();
    }

    private void RestoreSessionSettings()
    {
        SelectedLanguageIndex = _sessionState.CurrentLanguage == Language.CSharp ? 0 : 1;
        var timeoutIndex = Array.IndexOf(TimeoutOptions, _sessionState.TimeoutSeconds);
        SelectedTimeoutIndex = timeoutIndex >= 0 ? timeoutIndex : 2;
    }

    partial void OnSelectedLanguageIndexChanged(int value)
    {
        var newLang = value == 0 ? Language.CSharp : Language.VisualBasic;
        if (_sessionState.CurrentLanguage != newLang)
            _sessionState.CurrentLanguage = newLang;
    }

    partial void OnSelectedTimeoutIndexChanged(int value)
    {
        if (value >= 0 && value < TimeoutOptions.Length)
        {
            var newTimeout = TimeoutOptions[value];
            if (_sessionState.TimeoutSeconds != newTimeout)
                _sessionState.TimeoutSeconds = newTimeout;
        }
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
        if (string.IsNullOrWhiteSpace(InputText)) return;

        if (MetaCommandParser.IsMetaCommand(InputText))
        {
            ExecuteMetaCommand(InputText);
            InputText = "";
            return;
        }

        var language = SelectedLanguageIndex == 0 ? Language.CSharp : Language.VisualBasic;
        var langLabel = SelectedLanguageIndex == 0 ? "CSharp" : "VisualBasic";

        StatusText = $"Running ({langLabel})...";

        var inputLines = InputText.TrimEnd();
        ConversationItems.Add(new ConversationItem
        {
            DisplayText = $"> {inputLines}",
            IsInput = true
        });

        var result = _engine.Execute(InputText, language);

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

        _history.Record(inputLines, langLabel, outputText, !result.Success);

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

        if (result.Command == "clear")
        {
            ConversationItems.Clear();
            _history.Clear();
        }

        if (result.Command == "lang" && result.Success)
        {
            SelectedLanguageIndex = _sessionState.CurrentLanguage == Language.CSharp ? 0 : 1;
        }

        if (result.Command == "timeout" && result.Success)
        {
            var idx = Array.IndexOf(TimeoutOptions, _sessionState.TimeoutSeconds);
            if (idx >= 0)
                SelectedTimeoutIndex = idx;
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
            var timeoutLabel = _sessionState.TimeoutSeconds == 0 ? "No Limit" : $"{_sessionState.TimeoutSeconds}s";
            return $"Timeout: {timeoutLabel} | Refs: {_sessionState.ExtraReferences.Count} | Imports: {_sessionState.ExtraImports.Count}";
        }
    }
}
