# TASKS-14: Autocomplete Engine & Popup UI

**Block:** 14 de 16
**Depends on:** TASKS-2 (CompilationService), TASKS-9 (AssemblyResolutionService, dynamic refs), TASKS-11 (UI layout)
**PRD Reference:** `docs/PRD.md` — Seções 5.9 (Autocomplete Engine), 2.2 (US-11)

---

## Objective

Implementar autocomplete/IntelliSense completo: engine Roslyn no Core (AdhocWorkspace + CompletionService) e popup flutuante na UI (Avalonia Popup). Suporte a triggers automáticos (após `.` ou 3+ caracteres), manual (`Ctrl+Space`), debounce 300ms, cancelamento de requisições anteriores, e navegação por teclado. Incluir testes para o engine e para o comportamento do popup.

---

## Tasks

### 14.1 Criar modelo `CompletionItem`

Arquivo `src/QuickNET.Core/Models/CompletionItem.cs`:

```csharp
namespace QuickNET.Models;

public class CompletionItem
{
    public string DisplayText { get; init; } = "";
    public string InsertText { get; init; } = "";
    public string? Description { get; init; }
    public CompletionItemKind Kind { get; init; }
}

public enum CompletionItemKind
{
    Unknown,
    Keyword,
    Method,
    Property,
    Field,
    Class,
    Struct,
    Interface,
    Enum,
    Namespace,
    Variable,
    Snippet
}
```

### 14.2 Criar `CompletionEngine` (QuickNET.Core)

Criar diretório `src/QuickNET.Core/Completion/`.

Arquivo `src/QuickNET.Core/Completion/CompletionEngine.cs`:

```csharp
namespace QuickNET.Completion;

public class CompletionEngine
{
    private readonly AssemblyResolutionService _assemblyResolver;
    private AdhocWorkspace? _workspace;
    private Document? _currentDocument;
    private Project? _currentProject;
    private Language _workspaceLanguage;
    private IReadOnlyList<string>? _workspaceExtraReferences;
    private IReadOnlyList<string>? _workspaceExtraImports;

    public CompletionEngine(AssemblyResolutionService assemblyResolver)
    {
        _assemblyResolver = assemblyResolver;
    }

    public async Task<IReadOnlyList<CompletionItem>> GetCompletionsAsync(
        string sourceCode,
        int cursorPosition,
        Language language,
        IReadOnlyList<string>? extraReferences = null,
        IReadOnlyList<string>? extraImports = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var workspace = GetOrCreateWorkspace(sourceCode, language, extraReferences, extraImports);
        var document = workspace.CurrentSolution.Projects.First().Documents.First();

        // Update document text
        var sourceText = SourceText.From(sourceCode);
        document = document.WithText(sourceText);

        var completionService = CompletionService.GetService(document);
        if (completionService is null)
            return Array.Empty<CompletionItem>();

        ct.ThrowIfCancellationRequested();

        var completions = await completionService.GetCompletionsAsync(document, cursorPosition, cancellationToken: ct);
        if (completions is null)
            return Array.Empty<CompletionItem>();

        var defaultUsingImports = language == Language.CSharp
            ? new[] { "System", "System.Collections.Generic", "System.IO", "System.Linq", "System.Text", "System.Threading.Tasks" }
            : new[] { "System", "System.Collections.Generic", "System.IO", "System.Linq", "System.Text", "System.Threading.Tasks" };

        return completions.ItemsList
            .Where(i => !defaultUsingImports.Contains(i.DisplayTextPrefix)) // filtra namespaces já importados
            .Select(i => MapToCompletionItem(i))
            .ToList();
    }

    private AdhocWorkspace GetOrCreateWorkspace(
        string sourceCode, Language language,
        IReadOnlyList<string>? extraReferences, IReadOnlyList<string>? extraImports)
    {
        bool needsRecreate = _workspace is null
            || _workspaceLanguage != language
            || !ListsEqual(_workspaceExtraReferences, extraReferences)
            || !ListsEqual(_workspaceExtraImports, extraImports);

        if (needsRecreate)
        {
            _workspace = CreateWorkspace(sourceCode, language, extraReferences, extraImports);
            _workspaceLanguage = language;
            _workspaceExtraReferences = extraReferences?.ToList();
            _workspaceExtraImports = extraImports?.ToList();
        }

        return _workspace;
    }

    private AdhocWorkspace CreateWorkspace(
        string sourceCode, Language language,
        IReadOnlyList<string>? extraReferences, IReadOnlyList<string>? extraImports)
    {
        var workspace = new AdhocWorkspace();
        var projectInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            language == Language.CSharp ? "QuickNETCompletion-CSharp" : "QuickNETCompletion-VB",
            "QuickNETCompletion",
            language == Language.CSharp ? LanguageNames.CSharp : LanguageNames.VisualBasic);

        var project = workspace.AddProject(projectInfo);

        // Adicionar referências padrão + extras
        var allRefs = GetAllReferences(extraReferences);
        foreach (var mref in allRefs)
        {
            project = project.AddMetadataReference(mref);
        }

        // Adicionar imports via compilation options
        var allImports = GetAllImports(language, extraImports);
        if (language == Language.CSharp)
        {
            project = project.WithCompilationOptions(
                ((CSharpCompilationOptions)project.CompilationOptions!)
                    .WithUsings(allImports));
        }
        else
        {
            project = project.WithCompilationOptions(
                ((VisualBasicCompilationOptions)project.CompilationOptions!)
                    .WithGlobalImports(allImports.Select(i => GlobalImport.Parse(i))));
        }

        var document = project.AddDocument("code.csx", SourceText.From(sourceCode));
        _currentDocument = document;
        _currentProject = document.Project;

        return workspace;
    }

    private List<MetadataReference> GetAllReferences(IReadOnlyList<string>? extraReferences)
    {
        var refs = new List<MetadataReference>();

        // Referências padrão (mesmo conjunto do CompilationService)
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location)))
        {
            try { refs.Add(MetadataReference.CreateFromFile(asm.Location)); }
            catch { /* skip */ }
        }

        // Extras resolvidas via AssemblyResolutionService
        if (extraReferences is not null)
        {
            foreach (var name in extraReferences)
            {
                var resolved = _assemblyResolver.Resolve(name);
                if (resolved is not null)
                    refs.Add(resolved);
            }
        }

        return refs;
    }

    private List<string> GetAllImports(Language language, IReadOnlyList<string>? extraImports)
    {
        var imports = new List<string>
        {
            "System", "System.Collections.Generic", "System.IO",
            "System.Linq", "System.Text", "System.Threading.Tasks"
        };

        if (extraImports is not null)
            imports.AddRange(extraImports.Except(imports));

        return imports;
    }

    private static CompletionItem MapToCompletionItem(Microsoft.CodeAnalysis.Completion.CompletionItem item)
    {
        return new CompletionItem
        {
            DisplayText = item.DisplayText,
            InsertText = item.DisplayText,  // Roslyn fornece o texto correto com filtro aplicado
            Description = item.InlineDescription ?? item.GetDescriptionAsync().Result?.Text,
            Kind = MapKind(item.Tags)
        };
    }

    private static CompletionItemKind MapKind(ImmutableArray<string> tags)
    {
        if (tags.Contains("Keyword")) return CompletionItemKind.Keyword;
        if (tags.Contains("Method")) return CompletionItemKind.Method;
        if (tags.Contains("Property")) return CompletionItemKind.Property;
        if (tags.Contains("Field")) return CompletionItemKind.Field;
        if (tags.Contains("Class")) return CompletionItemKind.Class;
        if (tags.Contains("Struct")) return CompletionItemKind.Struct;
        if (tags.Contains("Interface")) return CompletionItemKind.Interface;
        if (tags.Contains("Enum")) return CompletionItemKind.Enum;
        if (tags.Contains("Namespace")) return CompletionItemKind.Namespace;
        return CompletionItemKind.Unknown;
    }

    private static bool ListsEqual(IReadOnlyList<string>? a, IReadOnlyList<string>? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.SequenceEqual(b);
    }
}
```

**Notas de implementação:**
- `GetOrCreateWorkspace` recria o workspace apenas quando referências, imports ou linguagem mudam. Atualizações de código (keystrokes) só atualizam o texto do documento (`document.WithText`), que é operação leve.
- O workspace usa projeto único com todas as referências disponíveis (padrão + extras). Isso garante que o autocomplete "enxergue" todos os tipos.
- As `using`/`Imports` globais são injetadas via `CompilationOptions` para que o Roslyn resolva tipos sem qualificação.
- `GetDescriptionAsync()` é chamado sincronamente (`.Result`) — para a v1.2 isso é aceitável. Se for muito lento, pode ser trocado para async posteriormente.

### 14.3 Registrar `CompletionEngine` no DI

Arquivo `src/QuickNET.Core/ServiceCollectionExtensions.cs`:

```csharp
services.AddSingleton<CompletionEngine>();
```

### 14.4 Criar Popup de Autocomplete (QuickNET.App)

Criar diretório `src/QuickNET.App/Controls/`.

Arquivo `src/QuickNET.App/Controls/CompletionPopup.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="QuickNET.Controls.CompletionPopup"
             Width="400" MaxHeight="200">
    <Border Background="{DynamicResource SystemControlBackgroundChromeMediumBrush}"
            BorderBrush="{DynamicResource SystemControlForegroundChromeDisabledBrush}"
            BorderThickness="1" CornerRadius="4"
            BoxShadow="0 4 12 rgba(0,0,0,0.3)">
        <ListBox x:Name="CompletionList"
                 ItemsSource="{Binding Items}"
                 SelectedItem="{Binding SelectedItem}"
                 Background="Transparent" BorderThickness="0"
                 VirtualizationMode="None">
            <ListBox.ItemTemplate>
                <DataTemplate>
                    <StackPanel Orientation="Horizontal" Spacing="8" Margin="4,2">
                        <TextBlock Text="{Binding KindIcon}" Width="16"
                                   Foreground="{DynamicResource SystemControlForegroundAccentBrush}" />
                        <TextBlock Text="{Binding DisplayText}"
                                   FontFamily="Cascadia Code, Consolas, monospace"
                                   FontSize="13" />
                        <TextBlock Text="{Binding Description}"
                                   Foreground="{DynamicResource SystemControlForegroundChromeDisabledBrush}"
                                   FontSize="11" Margin="16,0,0,0" />
                    </StackPanel>
                </DataTemplate>
            </ListBox.ItemTemplate>
        </ListBox>
    </Border>
</UserControl>
```

Arquivo `src/QuickNET.App/Controls/CompletionPopup.axaml.cs`:

```csharp
namespace QuickNET.Controls;

public partial class CompletionPopup : UserControl
{
    public CompletionPopup()
    {
        InitializeComponent();
    }

    public void MoveSelection(int delta)
    {
        var list = CompletionList;
        if (list.ItemCount == 0) return;

        var newIndex = (list.SelectedIndex + delta + list.ItemCount) % list.ItemCount;
        list.SelectedIndex = newIndex;
        list.ScrollIntoView(newIndex);
    }
}
```

**Nota:** O `CompletionPopup` é um `UserControl` (não um `Popup` diretamente). O `Popup` do Avalonia será criado no code-behind do `MainWindow` para encapsular este controle. Isso permite testar o UserControl isoladamente.

### 14.5 Criar `CompletionViewModel` (QuickNET.App)

Arquivo `src/QuickNET.App/ViewModels/CompletionViewModel.cs`:

```csharp
namespace QuickNET.ViewModels;

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
```

### 14.6 Integrar Autocomplete no `MainWindowViewModel`

Arquivo `src/QuickNET.App/ViewModels/MainWindowViewModel.cs`:

Adicionar novas dependências no construtor: `CompletionEngine`.

Adicionar campos e propriedades:

```csharp
private readonly CompletionEngine _completionEngine;
private CancellationTokenSource? _completionCts;
private DispatcherTimer? _completionDebounceTimer;

public CompletionViewModel Completion { get; } = new();
```

Adicionar método para solicitar completions com debounce:

```csharp
private void RequestCompletions(string code, int cursorPosition)
{
    // Cancela requisição anterior
    _completionCts?.Cancel();
    _completionCts?.Dispose();

    // Reseta timer de debounce
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
            // O posicionamento do popup é feito no MainWindow.axaml.cs
            CompletionRequested?.Invoke(this, EventArgs.Empty);
        }
    }
    catch (OperationCanceledException) { /* esperado */ }
    catch (Exception) { Completion.Hide(); }
}

// Evento para notificar a View que o popup deve ser mostrado/reposicionado
public event EventHandler? CompletionRequested;
```

**Trigger logic** — adicionar verificação no setter de `InputText` (ou no handler de `TextChanged` na View):

- Disparar automaticamente após digitar `.` (member access).
- Disparar automaticamente após o cursor estar após 3+ caracteres alfabéticos consecutivos (ex.: ao digitar `Cons`, completar `Console`).
- `Ctrl+Space` força a requisição independente da posição.

**Nota sobre trigger detection:** A lógica de trigger (verificar se o caractere antes do cursor é `.` ou se há 3+ letras) é implementada no `MainWindow.axaml.cs` (key handler) ou no ViewModel. Optar pelo ViewModel com um método `ShouldAutoTrigger(string code, int position)`.

### 14.7 Integrar Popup no `MainWindow.axaml` e `MainWindow.axaml.cs`

Arquivo `src/QuickNET.App/Views/MainWindow.axaml`:

Adicionar um `Popup` overlay:

```xml
<Grid RowDefinitions="*,Auto,Auto">
    <!-- Conversação (Row 0) -->
    <!-- Input (Row 1) -->
    <!-- Status bar (Row 2) -->

    <!-- Completion Popup overlay -->
    <Popup x:Name="CompletionOverlay"
           IsOpen="{Binding Completion.IsVisible}"
           PlacementTarget="{Binding #InputBox}"
           Placement="Top"
           HorizontalOffset="0" VerticalOffset="0"
           StaysOpen="False"
           Grid.RowSpan="3">
        <controls:CompletionPopup x:Name="CompletionPopupControl"
                                   DataContext="{Binding Completion}" />
    </Popup>
</Grid>
```

**Nota:** O posicionamento real do popup deve ser ajustado em code-behind para alinhar com a posição do caret, pois `PlacementTarget` + `Placement` estático no XAML não acompanha o caret dinamicamente.

Arquivo `src/QuickNET.App/Views/MainWindow.axaml.cs`:

Adicionar handlers:

```csharp
// No construtor ou OnDataContextChanged:
var vm = (MainWindowViewModel)DataContext!;
vm.CompletionRequested += OnCompletionRequested;

// Key handler para Ctrl+Space e navegação no popup:
private void InputBox_KeyDown(object? sender, KeyEventArgs e)
{
    var vm = (MainWindowViewModel)DataContext!;

    // Ctrl+Space: força autocomplete
    if (e.Key == Key.Space && e.KeyModifiers == KeyModifiers.Control)
    {
        vm.RequestCompletionsManually();
        e.Handled = true;
        return;
    }

    // Se o popup estiver aberto, setas e Enter navegam no popup
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
            case Key.Enter:
                vm.AcceptCompletion();
                e.Handled = true;
                return;
            case Key.Escape:
                vm.Completion.Hide();
                e.Handled = true;
                return;
        }
    }
}

// Posiciona o popup junto ao caret:
private void PositionCompletionPopup()
{
    var caretRect = InputBox.GetCaretRectangle();
    var translated = InputBox.TranslatePoint(caretRect.TopLeft, this) ?? default;
    // Converte para coordenadas relativas ao popup
    CompletionOverlay.HorizontalOffset = translated.X;
    CompletionOverlay.VerticalOffset = translated.Y - 200; // acima do caret (altura do popup)
}
```

**Fluxo de aceitação de completion (`AcceptCompletion`):**

```csharp
// No MainWindowViewModel:
public void AcceptCompletion()
{
    if (!Completion.IsVisible || Completion.SelectedItem is null) return;

    var insertText = Completion.SelectedItem.InsertText;
    var cursorPos = _inputBoxCaretPosition; // precisa ser rastreado

    // Insere o texto no InputText na posição do cursor
    var before = InputText[..cursorPos];
    var after = InputText[cursorPos..];
    InputText = before + insertText + after;

    Completion.Hide();
    // Reposiciona o cursor após o texto inserido
    CaretPositionChanged?.Invoke(this, cursorPos + insertText.Length);
}
```

**Rastreamento da posição do cursor:** A posição do cursor (`CaretIndex`) é mantida no `TextBox`. O `MainWindowViewModel` precisa saber a posição para inserir o completion e para enviar ao `CompletionEngine`. O valor é atualizado via binding ou evento no code-behind.

Adicionar no `MainWindow.axaml.cs`:

```csharp
InputBox.PropertyChanged += (s, e) =>
{
    if (e.Property == TextBox.CaretIndexProperty)
    {
        // Notificar ViewModel da posição do cursor
        vm.OnCaretPositionChanged(InputBox.CaretIndex);
    }
};
```

### 14.8 TextChanged trigger para autocomplete automático

No `MainWindow.axaml.cs`, assinar `InputBox.TextChanged` (ou `InputBox.KeyUp`):

```csharp
InputBox.TextChanged += (s, e) =>
{
    var text = InputBox.Text ?? "";
    var pos = InputBox.CaretIndex;

    if (ShouldAutoTrigger(text, pos))
    {
        vm.RequestCompletions(text, pos);
    }
    else
    {
        vm.Completion.Hide();
    }
};

private static bool ShouldAutoTrigger(string text, int position)
{
    if (position <= 0 || position > text.Length) return false;

    // Trigger após '.'
    if (position > 0 && text[position - 1] == '.')
        return true;

    // Trigger após 3+ caracteres alfabéticos consecutivos
    if (position >= 3)
    {
        int count = 0;
        for (int i = position - 1; i >= 0; i--)
        {
            if (char.IsLetter(text[i])) count++;
            else break;
        }
        if (count >= 3) return true;
    }

    return false;
}
```

### 14.9 Criar testes do Autocomplete

#### 14.9.1 `CompletionEngineTests`

Arquivo `tests/QuickNET.Tests/Completion/CompletionEngineTests.cs` (criar diretório `Completion/`):

Classe: `[TestClass] sealed`. Construtor injeta `AssemblyResolutionService` real e cria `CompletionEngine`.

| Método | Descrição |
|---|---|
| `GetCompletions_AfterDot_ReturnsMembers` | Simular código `"Console."` → lista contém `"WriteLine"` |
| `GetCompletions_KeywordPrefix_ReturnsKeywords` | Simular código `"usi"` → lista contém `"using"` |
| `GetCompletions_EmptyInput_NoCompletions` | `""` → lista vazia (sem trigger) |
| `GetCompletions_CancellationToken_CancelsOperation` | Passar token cancelado → `OperationCanceledException` |
| `GetCompletions_RespectsLanguage_CSharp` | `language = CSharp` → sugestões incluem keywords C# (`var`, `using`) |
| `GetCompletions_RespectsLanguage_VB` | `language = VisualBasic` → sugestões incluem keywords VB (`Dim`, `Module`) |
| `GetCompletions_WorkspaceReused_OnSameConfig` | Duas chamadas com mesmos refs/imports → workspace não recriado (verificar via performance ou estado interno) |
| `GetCompletions_WorkspaceRecreated_OnRefChange` | Chamada com refs A, depois com refs B → workspace recriado |
| `MapKind_MapsRoslynTagsToCompletionItemKind` | Testar `MapKind` com tags conhecidas (`"Keyword"`, `"Method"`, etc.) |

**Nota:** `CompletionService.GetCompletionsAsync` requer um documento válido em um projeto com referências. Nos testes, usar código suficientemente simples e referências que certamente existem no runtime de teste.

#### 14.9.2 `CompletionViewModelTests`

Arquivo `tests/QuickNET.Tests/ViewModels/CompletionViewModelTests.cs`:

Classe: `[TestClass] sealed`.

| Método | Descrição |
|---|---|
| `SetItems_NonEmpty_SetsVisible` | `SetItems([item1, item2])` → `IsVisible == true`, `Items.Count == 2` |
| `SetItems_Empty_HidesPopup` | `SetItems([])` → `IsVisible == false` |
| `SetItems_NullOrEmpty_SetsFirstSelected` | `SetItems([item1, item2])` → `SelectedItem == item1` |
| `Hide_ClearsItemsAndHides` | Chamar `Hide()` → `IsVisible == false`, `Items.Count == 0` |
| `CompletionItemViewModel_Kinds_MapCorrectly` | Conferir `KindIcon` para cada `CompletionItemKind` |

#### 14.9.3 `ShouldAutoTrigger` tests

Arquivo `tests/QuickNET.Tests/Completion/CompletionTriggerTests.cs`:

| Método | Descrição |
|---|---|
| `ShouldAutoTrigger_Dot_ReturnsTrue` | Texto `"Console."`, posição 8 → true |
| `ShouldAutoTrigger_ThreeLetters_ReturnsTrue` | Texto `"Con"`, posição 3 → true |
| `ShouldAutoTrigger_MoreThanThreeLetters_ReturnsTrue` | Texto `"Console"`, posição 7 → true |
| `ShouldAutoTrigger_TwoLetters_ReturnsFalse` | Texto `"Co"`, posição 2 → false |
| `ShouldAutoTrigger_EmptyInput_ReturnsFalse` | `""`, posição 0 → false |
| `ShouldAutoTrigger_PositionZero_ReturnsFalse` | `"abc"`, posição 0 → false |
| `ShouldAutoTrigger_BeforeDot_ReturnsFalse` | Texto `"Console"`, posição 7 (sem dot) → false (precisa de 3+ letras consecutivas) — wait, isso tem 7 letras, então true. |
| `ShouldAutoTrigger_LettersAfterPunctuation_ReturnsTrue` | Texto `"a = Cons"`, posição 7 → true (3 letras `"ons"`) |

**Nota:** A função `ShouldAutoTrigger` é definida no `MainWindow.axaml.cs`. Para testá-la, extrair como método `internal static` ou mover para uma classe helper testável.

---

## Acceptance Criteria

- [ ] `CompletionEngine.GetCompletionsAsync()` retorna sugestões via Roslyn `CompletionService`.
- [ ] Sugestões incluem keywords (`using`, `var`, `class` para C#; `Dim`, `Module` para VB) e membros de tipos (ex.: `Console.WriteLine`).
- [ ] Workspace é recriado apenas quando referências, imports ou linguagem mudam.
- [ ] Debounce de 300ms — requisições consecutivas cancelam a anterior.
- [ ] `CancellationToken` cancela a operação em andamento.
- [ ] `CompletionPopup` exibe lista scrollável com ícone de tipo, texto e descrição.
- [ ] Navegação no popup: setas ↑↓ movem seleção, Enter insere, Escape fecha.
- [ ] Autocomplete automático: popup aparece após `.` ou 3+ caracteres alfabéticos.
- [ ] Autocomplete manual: `Ctrl+Space` força abertura do popup.
- [ ] Popup fecha ao clicar fora ou ao continuar digitando (quando trigger não é mais satisfeito).
- [ ] `CompletionEngine` registrado como singleton no DI.
- [ ] `dotnet test` executa todos os testes sem falhas.
- [ ] Cobertura >= 70% nos novos módulos (`CompletionEngine`, `CompletionViewModel`, triggers).

---

## Notes for AI Agent

- **AdhocWorkspace:** Roslyn provê `AdhocWorkspace` para cenários sem arquivos em disco. É a abordagem correta para o QuickNET.
- **Performance:** A primeira chamada a `GetCompletionsAsync` é mais lenta (criação do workspace + compilação inicial). Chamadas subsequentes são rápidas (apenas atualização de texto). O debounce de 300ms mascara a latência da primeira chamada.
- **Description async:** `CompletionItem.GetDescriptionAsync()` é usado com `.Result` para simplificar. Se houver deadlocks, usar `await` com `ConfigureAwait(false)` dentro de uma task wrapper.
- **Referências no workspace:** O workspace precisa das mesmas referências de assembly que o `CompilationService` usa. Reutilizar `AssemblyResolutionService` para resolver referências extras. Para as padrão, iterar sobre `AppDomain.CurrentDomain.GetAssemblies()` (mesmo padrão do `CompilationService`).
- **Popup vs Flyout:** Usar `Popup` (não `Flyout`) porque Flyout é associado a um controle âncora e não permite posicionamento livre junto ao caret.
- **GetCaretRectangle():** Disponível no `TextBox` do Avalonia. Retorna as coordenadas do caret relativas ao próprio TextBox. `TranslatePoint()` converte para coordenadas da Window.
- **VirtualizationMode="None":** No `ListBox` do popup, desabilitar virtualização (`VirtualizationMode="None"`) porque a lista é pequena (< 50 itens tipicamente) e a virtualização pode causar glitches de altura.
- **Rastreamento de CaretIndex:** O `TextBox.CaretIndex` não é uma `StyledProperty` com binding fácil. Usar `PropertyChanged` event handler no code-behind para sincronizar com o ViewModel.
- **Teste de CompletionEngine:** O `AdhocWorkspace` e `CompletionService` funcionam em testes unitários (não requerem UI thread). Os testes devem ser `async Task` para suportar `GetCompletionsAsync`.
- **MSTest async:** Usar `[TestMethod]` com `public async Task MethodName()` — MSTest 4.x suporta nativamente.
