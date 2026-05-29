# TASKS-15: Input History Navigation

**Block:** 15 de 16
**Depends on:** TASKS-4 (History persistence), TASKS-11 (UI layout)
**PRD Reference:** `docs/PRD.md` — Seções 5.10 (Input History Navigation), 2.2 (US-12)

---

## Objective

Implementar navegação de histórico de inputs estilo terminal: setas ↑↓ percorrem os últimos 50 inputs executados, com preservação do rascunho atual, deduplicação consecutiva, e persistência entre sessões em `input-history.json`. Incluir testes para o serviço de histórico, persistência, e integração com a UI.

---

## Tasks

### 15.1 Criar `InputHistoryService` (QuickNET.Core)

Criar diretório `src/QuickNET.Core/History/` (se ainda não existir) ou colocar junto com os serviços de histórico existentes.

Arquivo `src/QuickNET.Core/History/InputHistoryService.cs`:

```csharp
namespace QuickNET.History;

public class InputHistoryService
{
    private const int MaxEntries = 50;
    private const string HistoryFileName = "input-history.json";

    private readonly List<string> _history = new(MaxEntries + 1);
    private int _navigationIndex = -1;    // -1 = não está navegando
    private string? _draft;               // rascunho salvo ao iniciar navegação
    private readonly string _filePath;

    public InputHistoryService()
    {
        _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuickNET",
            HistoryFileName);
        Load();
    }

    /// <summary>Construtor para testes com path customizado.</summary>
    internal InputHistoryService(string filePath)
    {
        _filePath = filePath;
        var dir = Path.GetDirectoryName(filePath)!;
        Directory.CreateDirectory(dir);
        Load();
    }

    /// <summary>Registra um input executado no histórico.</summary>
    public void Record(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return;

        // Deduplicação consecutiva: não adiciona se igual ao último
        if (_history.Count > 0 && _history[^1] == input)
            return;

        _history.Add(input);

        // Limite de 50 entradas (remove a mais antiga)
        while (_history.Count > MaxEntries)
            _history.RemoveAt(0);

        Reset();
        Save();
    }

    /// <summary>
    /// Navega para o input anterior (mais antigo).
    /// Retorna o texto do input histórico, ou null se não há mais entradas.
    /// </summary>
    public string? NavigateOlder(string currentDraft)
    {
        if (_history.Count == 0) return null;

        // Salva o rascunho atual na primeira navegação
        if (_navigationIndex == -1)
            _draft = currentDraft;

        if (_navigationIndex < _history.Count - 1)
            _navigationIndex++;

        return _history[^(1 + _navigationIndex)];
    }

    /// <summary>
    /// Navega para o input mais recente.
    /// Retorna o texto do input, ou o rascunho original ao ultrapassar o mais recente.
    /// Retorna null se não há mais entradas (já no rascunho e rascunho é vazio).
    /// </summary>
    public string? NavigateNewer()
    {
        if (_navigationIndex <= -1) return null;

        if (_navigationIndex > 0)
        {
            _navigationIndex--;
            return _history[^(1 + _navigationIndex)];
        }
        else
        {
            // Voltou ao ponto inicial — restaura o rascunho
            _navigationIndex = -1;
            var draft = _draft;
            _draft = null;
            return draft ?? "";
        }
    }

    /// <summary>Sai do modo de navegação e descarta o rascunho.</summary>
    public void Reset()
    {
        _navigationIndex = -1;
        _draft = null;
    }

    /// <summary>Retorna todas as entradas do histórico (mais antiga primeiro).</summary>
    public IReadOnlyList<string> GetEntries() => _history.AsReadOnly();

    /// <summary>Número de entradas no histórico.</summary>
    public int Count => _history.Count;

    private void Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var entries = JsonSerializer.Deserialize<List<string>>(json);
                if (entries is not null)
                {
                    _history.AddRange(entries);
                    while (_history.Count > MaxEntries)
                        _history.RemoveAt(0);
                }
            }
        }
        catch
        {
            // Arquivo corrompido ou inexistente — começa vazio
        }
    }

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_history, new JsonSerializerOptions
            {
                WriteIndented = false
            });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Falha ao salvar — não crasha
        }
    }
}
```

**Comportamento de navegação (exemplo):**

```
Histórico: ["2+2", "Console.WriteLine", "var x = 1"]  (índice 0 é o mais antigo)

Estado inicial: input vazio, _navigationIndex = -1, _draft = null

Usuário pressiona ↑:
  → NavigateOlder("") → salva _draft = "", _navigationIndex = 0, retorna "var x = 1"

Usuário pressiona ↑ de novo:
  → NavigateOlder(...) → _navigationIndex = 1, retorna "Console.WriteLine"

Usuário pressiona ↓:
  → NavigateNewer() → _navigationIndex = 0, retorna "var x = 1"

Usuário pressiona ↓ de novo:
  → NavigateNewer() → _navigationIndex = -1, restaura _draft = "", retorna ""
```

### 15.2 Registrar `InputHistoryService` no DI

Arquivo `src/QuickNET.Core/ServiceCollectionExtensions.cs`:

```csharp
services.AddSingleton<InputHistoryService>();
```

### 15.3 Integrar navegação no `MainWindowViewModel`

Arquivo `src/QuickNET.App/ViewModels/MainWindowViewModel.cs`:

Adicionar dependência `InputHistoryService` no construtor.

Adicionar métodos públicos para a View chamar:

```csharp
private readonly InputHistoryService _inputHistory;

/// <summary>Chamado pela View quando o usuário pressiona ↑.</summary>
public void NavigateHistoryOlder()
{
    var current = InputText ?? "";
    var result = _inputHistory.NavigateOlder(current);
    if (result is not null)
    {
        InputText = result;
        // Move o cursor para o final
        HistoryCursorMoved?.Invoke(this, result.Length);
    }
}

/// <summary>Chamado pela View quando o usuário pressiona ↓.</summary>
public void NavigateHistoryNewer()
{
    var result = _inputHistory.NavigateNewer();
    if (result is not null)
    {
        InputText = result;
        HistoryCursorMoved?.Invoke(this, result.Length);
    }
}

/// <summary>Registra o input atual no histórico após execução bem-sucedida.</summary>
public void RecordHistory(string input)
{
    _inputHistory.Record(input);
}

// Evento para a View reposicionar o cursor
public event EventHandler<int>? HistoryCursorMoved;
```

Atualizar `ExecuteCode()` para registrar o input no histórico:

```csharp
// Após executar o código com sucesso (não meta-commands):
_inputHistory.Record(input);
```

**Nota:** Meta-comandos **não** são registrados no histórico de inputs (ex.: `/help`, `/clear` não entram). Apenas código executado.

### 15.4 Integrar navegação no `MainWindow.axaml.cs`

Arquivo `src/QuickNET.App/Views/MainWindow.axaml.cs`:

Adicionar handler de `KeyDown` no `InputBox` para as setas ↑↓:

```csharp
InputBox.KeyDown += (s, e) =>
{
    var vm = (MainWindowViewModel)DataContext!;

    // Se o popup de autocomplete estiver aberto, setas navegam no popup (não no histórico)
    if (vm.Completion.IsVisible)
        return; // já tratado em TASKS-14

    if (e.Key == Key.Up)
    {
        vm.NavigateHistoryOlder();
        e.Handled = true;
    }
    else if (e.Key == Key.Down)
    {
        vm.NavigateHistoryNewer();
        e.Handled = true;
    }
};
```

Assinar evento `HistoryCursorMoved` para reposicionar o cursor:

```csharp
vm.HistoryCursorMoved += (_, pos) =>
{
    InputBox.CaretIndex = pos;
};
```

**Nota sobre coexistência com Enter:** O handler de `Key.Up`/`Key.Down` deve ser registrado **antes** ou **junto com** o handler de `Enter` existente (`KeyDown` no `MainWindow.axaml.cs`). Certificar-se de que `e.Handled = true` previne comportamentos indesejados (ex.: mover o cursor no TextBox).

### 15.5 Reset da navegação ao editar manualmente

Quando o usuário navega no histórico (↑↓) e depois digita algo manualmente, a navegação deve ser cancelada. Isso é detectado no setter de `InputText` ou via `TextChanged`:

```csharp
// No MainWindow.axaml.cs, handler de TextChanged:
InputBox.TextChanged += (s, e) =>
{
    // Se o texto mudou e não foi via navegação do histórico nem aceitação de completion,
    // reseta a navegação
    if (!_isNavigatingHistory && !_isAcceptingCompletion)
    {
        vm.ResetHistoryNavigation();
    }
    _isNavigatingHistory = false;
    _isAcceptingCompletion = false;
};
```

Adicionar flags `_isNavigatingHistory` e `_isAcceptingCompletion` no code-behind, setadas antes de chamar `NavigateHistoryOlder`/`NavigateHistoryNewer`/`AcceptCompletion`.

Alternativa mais simples: resetar a navegação **antes** de modificar `InputText` por qualquer fonte que não seja histórico.

Adicionar ao `MainWindowViewModel`:

```csharp
public void ResetHistoryNavigation()
{
    _inputHistory.Reset();
}
```

### 15.6 Criar testes do Input History

#### 15.6.1 `InputHistoryServiceTests`

Arquivo `tests/QuickNET.Tests/History/InputHistoryServiceTests.cs`:

Classe: `[TestClass] sealed`. Construtor cria `InputHistoryService` com path temporário.

| Método | Descrição |
|---|---|
| `Record_NewInput_AddsToHistory` | `Record("2+2")` → `GetEntries().Count == 1`, contém `"2+2"` |
| `Record_DuplicateConsecutive_Ignored` | `Record("a")`, `Record("a")` → `Count == 1` |
| `Record_SameButNotConsecutive_Added` | `Record("a")`, `Record("b")`, `Record("a")` → `Count == 3` |
| `Record_ExceedsMax_DropsOldest` | Adicionar 51 entradas → `Count == 50`, primeira entrada removida |
| `Record_WhitespaceOrEmpty_Ignored` | `Record("")`, `Record("  ")` → `Count == 0` |
| `NavigateOlder_EmptyHistory_ReturnsNull` | Sem entradas → `NavigateOlder("draft")` retorna null |
| `NavigateOlder_ReturnsMostRecentEntry` | `Record("1")`, `Record("2")` → `NavigateOlder("")` retorna `"2"` |
| `NavigateOlder_Twice_ReturnsSecondMostRecent` | `Record("1")`, `Record("2")`, `Record("3")` → 2x `NavigateOlder` retorna `"2"` |
| `NavigateOlder_AtEnd_ReturnsSameEntry` | Apenas 1 entrada, chamar 2x → segunda chamada retorna a mesma entrada |
| `NavigateNewer_AfterOlder_MovesBack` | `NavigateOlder`, depois `NavigateNewer` → retorna entrada mais recente |
| `NavigateNewer_AtMostRecent_RestoresDraft` | `NavigateOlder("meu draft")`, 1x `NavigateNewer`, 1x `NavigateNewer` → restaura `"meu draft"` |
| `DraftPreserved_WhenNavigating` | Input atual `"draft123"`, `NavigateOlder("draft123")` → retorna entrada histórica. `NavigateNewer` 2x → retorna `"draft123"` |
| `Reset_ExitsNavigation` | `NavigateOlder("x")` → `Reset()` → `NavigateNewer()` retorna null |
| `Reset_ClearsDraft` | `NavigateOlder("draft")` → `Reset()` → próximo `NavigateNewer` retorna null |
| `Record_ResetsNavigation` | `NavigateOlder("x")` → `Record("new")` → `NavigateNewer()` retorna null |
| `Load_ExistingFile_LoadsHistory` | Criar `input-history.json` com `["a","b","c"]` → depois de carregar, `GetEntries()` contém os 3 |
| `Load_MissingFile_EmptyHistory` | Path para arquivo inexistente → `Count == 0`, sem crash |
| `Load_CorruptFile_EmptyHistory` | Arquivo com JSON inválido → `Count == 0`, sem crash |
| `Save_CreatesFile` | `Record("test")` → arquivo `input-history.json` existe e contém `"test"` |
| `Save_MaxEntriesInFile` | 51 `Record`s → arquivo contém apenas 50 entradas |

#### 15.6.2 `MainWindowViewModelHistoryTests`

Adicionar ao arquivo `tests/QuickNET.Tests/ViewModels/MainWindowViewModelTests.cs`:

| Método | Descrição |
|---|---|
| `ExecuteCode_RecordsInputInHistory` | Executar `"2 + 2"` → `InputHistoryService.GetEntries()` contém `"2 + 2"` |
| `ExecuteCode_MetaCommand_NotRecorded` | Executar `/help` → histórico de inputs não contém `/help` |
| `NavigateHistoryOlder_UpdatesInputText` | Histórico com `["prev"]` → `NavigateHistoryOlder()` → `InputText == "prev"` |
| `NavigateHistoryNewer_AfterOlder_RestoresDraft` | `InputText = "draft"`, `NavigateHistoryOlder()`, `NavigateHistoryNewer()` → `InputText == "draft"` |
| `ResetHistoryNavigation_AfterNavigate_Resets` | `NavigateHistoryOlder()` → `ResetHistoryNavigation()` → `NavigateHistoryNewer()` retorna vazio |

#### 15.6.3 `InputHistoryPersistenceTests`

Arquivo `tests/QuickNET.Tests/History/InputHistoryPersistenceTests.cs`:

| Método | Descrição |
|---|---|
| `Persist_AfterRecord_FileExists` | `Record("hello")` → arquivo `input-history.json` existe |
| `Persist_RoundTrip_PreservesEntries` | `Record("a")`, `Record("b")` → novo `InputHistoryService` com mesmo path → `GetEntries()` contém ambos |
| `Persist_Deduplicated_OnSave` | 3x `Record("same")` → arquivo contém apenas 1 entrada |

---

## Acceptance Criteria

- [ ] `InputHistoryService` mantém buffer circular das últimas 50 entradas únicas consecutivas.
- [ ] Seta ↑ navega para inputs mais antigos; seta ↓ navega para inputs mais recentes.
- [ ] Rascunho atual (se houver) é preservado ao navegar e restaurado ao voltar.
- [ ] Ao submeter um input (Enter), ele é registrado no histórico e a navegação reseta.
- [ ] Meta-comandos **não** são registrados no histórico de inputs.
- [ ] Ao digitar manualmente durante a navegação, a navegação é cancelada.
- [ ] Histórico persiste em `%APPDATA%\QuickNET\input-history.json`.
- [ ] Arquivo corrompido ou inexistente não causa crash (inicia vazio).
- [ ] Setas ↑↓ no popup de autocomplete navegam no popup, não no histórico (coexistência).
- [ ] `InputHistoryService` registrado como singleton no DI.
- [ ] `dotnet test` executa todos os testes sem falhas.
- [ ] Cobertura >= 70% no `InputHistoryService`.

---

## Notes for AI Agent

- **Coexistência com autocomplete:** No `MainWindow.axaml.cs`, verificar `vm.Completion.IsVisible` antes de processar setas como navegação de histórico. Se o popup estiver aberto, as setas navegam no popup (TASKS-14).
- **Coexistência com TextBox nativo:** O `TextBox` do Avalonia tem comportamento padrão para `Key.Up`/`Key.Down` (move o cursor entre linhas). Fazer `e.Handled = true` é essencial para prevenir esse comportamento quando estamos navegando no histórico.
- **Persistência de input-history.json:** O formato é um JSON array simples (`["input1", "input2", ...]`). Sem wrapping em objeto, sem metadados. Minimalista e fácil de debugar.
- **Deduplicação consecutiva:** Se o usuário executar `"2+2"` três vezes seguidas, só a primeira entra. Se executar `"2+2"`, depois `"3+3"`, depois `"2+2"` de novo, ambas as execuções de `"2+2"` entram (não são consecutivas).
- **Reset ao digitar:** O método mais confiável é resetar no `TextChanged` quando a mudança não veio de `NavigateHistoryOlder`/`NavigateHistoryNewer`/`AcceptCompletion`. Usar flags booleanas no code-behind para rastrear a origem da mudança.
- **CaretIndex após navegação:** Após `InputText = result`, o cursor do TextBox vai para o início. O evento `HistoryCursorMoved` notifica o code-behind para setar `InputBox.CaretIndex = result.Length` (final do texto).
- **InternalsVisibleTo:** O construtor `internal InputHistoryService(string filePath)` já é acessível nos testes (configurado em TASKS-12).
- **Testes com path temporário:** Mesmo padrão dos `SessionStateTests` — `Path.Combine(Path.GetTempPath(), "QuickNET_Tests", Guid.NewGuid().ToString())` e cleanup no `[TestCleanup]`.
