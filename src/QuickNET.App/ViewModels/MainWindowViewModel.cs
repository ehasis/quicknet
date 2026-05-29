using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickNET.App.Models;
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

    public event EventHandler? CloseRequested;

    [ObservableProperty]
    private string _inputText = "";

    [ObservableProperty]
    private int _selectedLanguageIndex = 0;

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<ConversationItem> ConversationItems { get; } = [];

    public MainWindowViewModel(ReplEngine engine, HistoryService history,
        MetaCommandService metaCommandService, SessionState sessionState,
        ThemeService themeService)
    {
        _engine = engine;
        _history = history;
        _metaCommandService = metaCommandService;
        _sessionState = sessionState;
        _themeService = themeService;
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
}
