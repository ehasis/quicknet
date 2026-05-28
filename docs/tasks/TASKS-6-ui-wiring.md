# TASKS-6: UI Wiring — ViewModels, DI, & Interaction Logic

**Block:** 6 de 7
**Depends on:** TASKS-2, TASKS-3, TASKS-4, TASKS-5
**PRD Reference:** `docs/PRD.md` — Seções 2.2, 4.2, 4.3

---

## Objective

Conectar a UI do Avalonia ao engine do QuickNET: ViewModels com bindings reais, DI para injeção de serviços, comandos de execução, e key bindings (Enter para single-line, Shift+Enter para multi-line).

---

## Tasks

### 6.1 Criar ConversationItem (ViewModel display model)

Arquivo `src/QuickNET.App/Models/ConversationItem.cs`:

```csharp
namespace QuickNET.App.Models;

public class ConversationItem
{
    public string DisplayText { get; set; } = "";
    public bool IsInput { get; set; }    // true = "> código", false = output
    public bool IsError { get; set; }
}
```

### 6.2 Criar MainWindowViewModel

Arquivo `src/QuickNET.App/ViewModels/MainWindowViewModel.cs`:

```csharp
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
    private int _selectedLanguageIndex = 0; // 0 = CSharp, 1 = VisualBasic

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

        // Add input to conversation
        var inputLines = InputText.TrimEnd();
        ConversationItems.Add(new ConversationItem
        {
            DisplayText = $"> {inputLines}",
            IsInput = true
        });

        // Execute
        var result = _engine.Execute(InputText, language);

        // Add output to conversation
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

        // Persist history
        _history.Record(inputLines, langLabel, outputText, !result.Success);

        // Clear input and update status
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
```

**Pacote necessário:** `CommunityToolkit.Mvvm` (source generators para `[ObservableProperty]` e `[RelayCommand]`)

```pwsh
dotnet add src/QuickNET.App package CommunityToolkit.Mvvm
```

### 6.3 Atualizar MainWindow.axaml com bindings

Substituir o conteúdo por:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="clr-namespace:QuickNET.App.ViewModels"
        x:Class="QuickNET.App.Views.MainWindow"
        x:DataType="vm:MainWindowViewModel"
        Title="QuickNET"
        Width="900" Height="620"
        MinWidth="600" MinHeight="400"
        WindowStartupLocation="CenterScreen">

    <Grid RowDefinitions="Auto,*,Auto,Auto">
        <!-- Toolbar -->
        <StackPanel Grid.Row="0" Orientation="Horizontal"
                    Margin="8,6" Spacing="10">
            <TextBlock Text="Lang:" VerticalAlignment="Center" />
            <ComboBox Width="120"
                      SelectedIndex="{Binding SelectedLanguageIndex}">
                <ComboBoxItem>C#</ComboBoxItem>
                <ComboBoxItem>VB.NET</ComboBoxItem>
            </ComboBox>
            <Button Content="Clear"
                    Command="{Binding ClearHistoryCommand}" />
        </StackPanel>

        <!-- Conversation Panel -->
        <ScrollViewer Grid.Row="1"
                      x:Name="ConversationScroller"
                      AllowAutoHide="False">
            <ItemsControl ItemsSource="{Binding ConversationItems}"
                          Margin="8">
                <ItemsControl.ItemTemplate>
                    <DataTemplate x:DataType="models:ConversationItem"
                                  xmlns:models="clr-namespace:QuickNET.App.Models">
                        <StackPanel Margin="0,2">
                            <TextBlock Text="{Binding DisplayText}"
                                       FontFamily="Cascadia Code, Consolas, monospace"
                                       FontSize="13"
                                       Foreground="{Binding IsError, Converter={StaticResource ErrorColorConverter}}" />
                        </StackPanel>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>

        <!-- Input Area -->
        <Border Grid.Row="2"
                BorderBrush="{DynamicResource SystemControlForegroundBaseMediumBrush}"
                BorderThickness="0,1,0,0"
                Padding="8,4">
            <DockPanel>
                <StackPanel DockPanel.Dock="Bottom" Orientation="Horizontal"
                            Margin="0,4,0,0">
                    <TextBlock Text="Shift+Enter to run"
                               Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"
                               FontSize="11"
                               VerticalAlignment="Center" />
                </StackPanel>
                <TextBox Text="{Binding InputText}"
                         AcceptsReturn="True"
                         TextWrapping="Wrap"
                         FontFamily="Cascadia Code, Consolas, monospace"
                         FontSize="14"
                         MinHeight="28"
                         MaxHeight="200"
                         Watermark="Type your code here..." />
            </DockPanel>
        </Border>

        <!-- Status Bar -->
        <Border Grid.Row="3"
                Background="{DynamicResource SystemControlBackgroundBaseLowBrush}"
                Padding="8,2">
            <TextBlock Text="{Binding StatusText}"
                       FontSize="11" />
        </Border>
    </Grid>
</Window>
```

### 6.4 Implementar key bindings (Enter / Shift+Enter)

No code-behind `MainWindow.axaml.cs`:

```csharp
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
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (e.KeyModifiers == KeyModifiers.None)
            {
                // Single-line: executar diretamente
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.ExecuteCodeCommand.Execute(null);
                }
                e.Handled = true;
            }
            else if (e.KeyModifiers == KeyModifiers.Shift)
            {
                // Multi-line: submeter bloco inteiro
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.ExecuteCodeCommand.Execute(null);
                }
                e.Handled = true;
            }
        }
    }
}
```

### 6.5 Configurar DI no Program.cs

Atualizar `src/QuickNET.App/Program.cs`:

```csharp
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using QuickNET.App.ViewModels;
using QuickNET.App.Views;
using System;

namespace QuickNET.App;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddQuickNETCore();
        services.AddSingleton<MainWindowViewModel>();

        var provider = services.BuildServiceProvider();

        BuildAvaloniaApp(provider).StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp(IServiceProvider? serviceProvider = null)
        => AppBuilder.Configure(() => new App(serviceProvider))
            .UsePlatformDetect()
            .LogToTrace();
}
```

Atualizar `App.axaml.cs` para receber o provider:

```csharp
public class App : Application
{
    private readonly IServiceProvider? _serviceProvider;

    public App() { }

    public App(IServiceProvider? serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            if (_serviceProvider != null)
            {
                mainWindow.DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            }
            desktop.MainWindow = mainWindow;
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

Necessário adicionar `using Microsoft.Extensions.DependencyInjection;` e `using QuickNET;` no App.axaml.cs.

### 6.6 Adicionar AutoScroll ao painel de conversação

No `MainWindow.axaml.cs`, adicionar após `InitializeComponent()`:

```csharp
ConversationItems.CollectionChanged += (_, _) =>
{
    ConversationScroller.ScrollToEnd();
};
```

Para isso, expor `ConversationScroller` como field e acessar via `x:Name`.

---

## Acceptance Criteria

- [ ] `dotnet run --project src/QuickNET.App` abre a janela com DI funcionando.
- [ ] Digitar `2 + 2` e pressionar `Enter` exibe `> 2 + 2` seguido de `4` no painel de conversação.
- [ ] Digitar código multi-linha (ex.: `var x = 10;\nvar y = x * 2;\nreturn y;`) e pressionar `Shift+Enter` executa o bloco completo e exibe o resultado.
- [ ] Código inválido exibe erro em vermelho no painel (ex.: `2 +` mostra mensagem de erro de compilação).
- [ ] Trocar linguagem no ComboBox e executar `2 + 2` em VB.NET funciona corretamente.
- [ ] O botão Clear limpa o painel de conversação e o histórico.
- [ ] Fechar e reabrir a aplicação restaura o histórico anterior.
- [ ] O painel de conversação faz auto-scroll para a última entrada.
- [ ] `Console.WriteLine("hello")` exibe "hello" antes do valor de retorno.

---

## Notes for AI Agent

- `CommunityToolkit.Mvvm` v8.x usa source generators. O `[ObservableProperty]` gera a propriedade pública com nome PascalCase a partir do campo `_camelCase`. Ex.: `_inputText` → propriedade `InputText`.
- `[RelayCommand]` gera `ExecuteCodeCommand` e `ClearHistoryCommand` automaticamente.
- O `x:DataType` nos elementos XAML habilita compiled bindings (melhor performance e erros em tempo de compilação).
- Se compiled bindings causarem erros com `StaticResource`, usar `DynamicResource` em vez disso.
- O `ConversationScroller.ScrollToEnd()` precisa ser chamado após o item ser renderizado. Se não funcionar imediatamente, envolver em `Dispatcher.UIThread.InvokeAsync(() => ConversationScroller.ScrollToEnd())`.
- Para erro de compilação vs runtime: ambos são tratados em `ReplEngine.Execute()`. Erro de compilação retorna `Success = false` com os diagnostics em `Error`. Erro de runtime retorna `Success = false` com a exceção em `Error`.
- O namespace `QuickNET` do Core deve ser importado via `using QuickNET;` nos arquivos de UI que usam `ReplEngine`, `Language`, etc.
