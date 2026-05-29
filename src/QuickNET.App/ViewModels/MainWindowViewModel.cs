using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using QuickNET.App.Models;
using QuickNET.History;
using QuickNET.Models;

namespace QuickNET.App.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly ReplEngine _engine;
    private readonly HistoryService _history;

    [ObservableProperty]
    private string _inputText = "";

    [ObservableProperty]
    private int _selectedLanguageIndex = 0;

    [ObservableProperty]
    private string _statusText = "Ready";

    public ObservableCollection<ConversationItem> ConversationItems { get; } = [];

    public MainWindowViewModel(ReplEngine engine, HistoryService history)
    {
        _engine = engine;
        _history = history;
        LoadHistory();
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

    [RelayCommand]
    private void ClearHistory()
    {
        ConversationItems.Clear();
        _history.Clear();
        StatusText = "History cleared";
    }
}
