# TASKS-11: UI Updates for v1.1

**Block:** 11 de 12
**Depends on:** TASKS-5 (UI shell), TASKS-6 (ViewModel), TASKS-8 (SessionState, MetaCommandService), TASKS-9, TASKS-10
**PRD Reference:** `docs/PRD.md` — Seções 4.1, 4.2, 5.4, 5.7

---

## Objective

Atualizar a UI para expor as novas funcionalidades da v1.1: ComboBox de timeout na toolbar, status bar informativa (timeout, refs, imports), sincronização bidirecional entre meta-comandos e controles da UI, e roteamento de meta-comandos no ViewModel.

---

## Tasks

### 11.1 Atualizar MainWindowViewModel — timeout e sync

Arquivo `src/QuickNET.App/ViewModels/MainWindowViewModel.cs`:

#### 11.1.1 Adicionar campos para timeout

```csharp
// Mapeamento entre índice do ComboBox e segundos de timeout
private static readonly int[] TimeoutOptions = [5, 10, 30, 60, 0]; // 0 = no limit

[ObservableProperty]
private int _selectedTimeoutIndex = 2; // default: 30s (índice 2)
```

#### 11.1.2 Injetar novas dependências

Adicionar `MetaCommandService` e `SessionState` ao construtor:

```csharp
private readonly MetaCommandService _metaCommandService;
private readonly SessionState _sessionState;

public MainWindowViewModel(
    ReplEngine engine,
    HistoryService history,
    MetaCommandService metaCommandService,
    SessionState sessionState)
{
    _engine = engine;
    _history = history;
    _metaCommandService = metaCommandService;
    _sessionState = sessionState;
    LoadHistory();
    RestoreSessionSettings();
}
```

#### 11.1.3 Implementar RestoreSessionSettings

```csharp
private void RestoreSessionSettings()
{
    // Restore language
    _selectedLanguageIndex = _sessionState.CurrentLanguage == Language.CSharp ? 0 : 1;

    // Restore timeout
    _selectedTimeoutIndex = Array.IndexOf(TimeoutOptions, _sessionState.TimeoutSeconds);
    if (_selectedTimeoutIndex < 0) _selectedTimeoutIndex = 2; // fallback to 30s
}
```

#### 11.1.4 Implementar sync bidirecional: ComboBox → SessionState

Adicionar `partial void` para `OnSelectedLanguageIndexChanged` e `OnSelectedTimeoutIndexChanged`:

```csharp
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
```

#### 11.1.5 Atualizar ExecuteCode para rotear meta-comandos

Modificar o método `ExecuteCode` para verificar meta-comandos antes do pipeline normal:

```csharp
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

    // === pipeline normal existente (compilação + execução) ===
    var language = SelectedLanguageIndex == 0 ? Language.CSharp : Language.VisualBasic;
    // ... resto do código existente sem alterações ...
}

private void ExecuteMetaCommand(string input)
{
    var result = _metaCommandService.Execute(input);

    ConversationItems.Add(new ConversationItem
    {
        DisplayText = $"> {input.TrimEnd()}",
        IsInput = true
    });

    // Side-effect: /clear
    if (result.Command == "clear")
    {
        ConversationItems.Clear();
        _history.Clear();
    }

    // Side-effect: /lang — sync ComboBox
    if (result.Command == "lang" && result.Success)
    {
        _selectedLanguageIndex = _sessionState.CurrentLanguage == Language.CSharp ? 0 : 1;
        OnPropertyChanged(nameof(SelectedLanguageIndex));
    }

    // Side-effect: /timeout — sync ComboBox
    if (result.Command == "timeout" && result.Success)
    {
        var idx = Array.IndexOf(TimeoutOptions, _sessionState.TimeoutSeconds);
        if (idx >= 0)
        {
            _selectedTimeoutIndex = idx;
            OnPropertyChanged(nameof(SelectedTimeoutIndex));
        }
    }

    ConversationItems.Add(new ConversationItem
    {
        DisplayText = result.DisplayText,
        IsInput = false,
        IsError = !result.Success
    });

    StatusText = result.Success ? "Ready" : "Error";
}
```

### 11.2 Atualizar MainWindow.axaml — Timeout ComboBox

Arquivo `src/QuickNET.App/Views/MainWindow.axaml`:

Adicionar o ComboBox de timeout na toolbar, ao lado do seletor de linguagem:

```xml
<!-- Toolbar -->
<StackPanel Grid.Row="0" Orientation="Horizontal"
            Margin="8,6" Spacing="10">
    <TextBlock Text="Lang:" VerticalAlignment="Center" />
    <ComboBox Width="120"
              SelectedIndex="{Binding SelectedLanguageIndex}">
        <ComboBoxItem>C#</ComboBoxItem>
        <ComboBoxItem>VB.NET</ComboBoxItem>
    </ComboBox>

    <!-- Novo: Timeout selector -->
    <TextBlock Text="Timeout:" VerticalAlignment="Center" Margin="16,0,0,0" />
    <ComboBox Width="100"
              SelectedIndex="{Binding SelectedTimeoutIndex}">
        <ComboBoxItem>5s</ComboBoxItem>
        <ComboBoxItem>10s</ComboBoxItem>
        <ComboBoxItem>30s</ComboBoxItem>
        <ComboBoxItem>60s</ComboBoxItem>
        <ComboBoxItem>No Limit</ComboBoxItem>
    </ComboBox>

    <Button Content="Clear"
            Command="{Binding ClearHistoryCommand}"
            HorizontalAlignment="Right" />
</StackPanel>
```

### 11.3 Atualizar status bar com informações de sessão

Modificar a status bar para mostrar informações úteis sobre o estado da sessão. No `MainWindow.axaml`:

```xml
<!-- Status Bar -->
<Border Grid.Row="3"
        Background="{DynamicResource SystemControlBackgroundBaseLowBrush}"
        Padding="8,2">
    <StackPanel Orientation="Horizontal" Spacing="16">
        <TextBlock Text="{Binding StatusText}" FontSize="11" />
        <!-- Pode ser enriquecido com refs/imports count via binding adicional -->
    </StackPanel>
</Border>
```

**Opcional:** Criar uma propriedade calculada `SessionInfoText` no ViewModel que agrega timeout + refs + imports em uma string, exibida na status bar:

```csharp
public string SessionInfoText => $"Timeout: {TimeoutLabel} | Refs: {_sessionState.ExtraReferences.Count} | Imports: {_sessionState.ExtraImports.Count}";
```

Vincular um segundo `TextBlock` na status bar a esta propriedade. Atualizar via `OnPropertyChanged(nameof(SessionInfoText))` após mutações relevantes (após `/reference`, `/import`, `/timeout`).

### 11.4 Atualizar DI no Program.cs

Arquivo `src/QuickNET.App/Program.cs`:

Verificar que `MainWindowViewModel` continua sendo resolvido corretamente com as novas dependências. O `ServiceCollection` já registra `MetaCommandService` e `SessionState` via `AddQuickNETCore()`. O `MainWindowViewModel` deve ser registrado após `AddQuickNETCore()`:

```csharp
var services = new ServiceCollection();
services.AddQuickNETCore();
services.AddSingleton<MainWindowViewModel>(); // DI resolve MetaCommandService e SessionState automaticamente
```

### 11.5 Atualizar code-behind do MainWindow (se necessário)

Arquivo `src/QuickNET.App/Views/MainWindow.axaml.cs`:

Verificar se o handler de `KeyDown` existente (Enter / Shift+Enter) continua funcionando com o novo `ExecuteCode`. Como `ExecuteCode` agora verifica `MetaCommandParser.IsMetaCommand()` internamente, nenhuma alteração é necessária no code-behind — o roteamento acontece no ViewModel.

---

## Acceptance Criteria

- [ ] O ComboBox de timeout está visível na toolbar com as 5 opções (5s, 10s, 30s, 60s, No Limit).
- [ ] Selecionar "10s" no ComboBox persiste a escolha no `settings.json`.
- [ ] Fechar e reabrir a aplicação restaura o timeout selecionado anteriormente.
- [ ] Digitar `/timeout 5` atualiza o ComboBox para "5s" em tempo real.
- [ ] Selecionar "60s" no ComboBox faz com que `/timeout` (sem args) mostre "Current timeout: 60s".
- [ ] Digitar `/lang vb` atualiza o ComboBox de linguagem para "VB.NET".
- [ ] Selecionar "VB.NET" no ComboBox faz com que `/lang` (sem args) mostre "Current language: VisualBasic".
- [ ] Digitar um meta-comando (`/help`, `/clear`, etc.) e pressionar Enter executa o comando e não tenta compilar.
- [ ] `/clear` limpa o painel de conversação e o histórico.
- [ ] A status bar reflete o estado atual (Ready/Error + informações de sessão).
- [ ] O pipeline normal de execução (código sem `/`) continua funcionando inalterado.

---

## Notes for AI Agent

- O `[ObservableProperty]` do CommunityToolkit.Mvvm gera automaticamente os métodos `partial void On<PropertyName>Changed`. Estes são o local correto para sincronizar com `SessionState`.
- A sincronização bidirecional deve evitar loops infinitos:
  - ComboBox muda → `OnSelectedTimeoutIndexChanged` → atualiza `SessionState.TimeoutSeconds` → `Save()`.
  - `/timeout` → `SessionState.TimeoutSeconds` muda → ViewModel atualiza `_selectedTimeoutIndex` → `OnPropertyChanged` → ComboBox atualiza → `OnSelectedTimeoutIndexChanged` dispara → mas `_sessionState.TimeoutSeconds` já está correto, então o setter não chama `Save()` de novo (verificar se o valor mudou).
  - Para evitar ciclos, a implementação de `OnSelectedTimeoutIndexChanged` deve verificar se o valor já está correto antes de atribuir a `SessionState`.
- O `OnPropertyChanged(nameof(SelectedLanguageIndex))` e `OnPropertyChanged(nameof(SelectedTimeoutIndex))` são necessários porque `_selectedLanguageIndex` e `_selectedTimeoutIndex` são campos backing fields — ao setá-los diretamente (sem passar pelo property setter), o binding não notifica a UI. Alternativamente, usar o property setter (que notifica automaticamente).
- O layout do toolbar usa `StackPanel` horizontal. O espaçamento entre elementos usa `Margin` e `Spacing`. Manter consistência com o layout existente.
- `using QuickNET.MetaCommands;` necessário no ViewModel para acessar `MetaCommandParser`.
- `using QuickNET.Session;` necessário para acessar `SessionState` (embora possa ser implícito se ambos estiverem no namespace `QuickNET`).
