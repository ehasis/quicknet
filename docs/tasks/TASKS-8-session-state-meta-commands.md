# TASKS-8: Session State & Meta-command Engine

**Block:** 8 de 12
**Depends on:** TASKS-2 (CompilationService), TASKS-4 (HistoryService), TASKS-6 (ViewModel)
**PRD Reference:** `docs/PRD.md` — Seções 5.4, 5.5

---

## Objective

Implementar o sistema de estado de sessão persistente (`SessionState`) e o motor de meta-comandos (`MetaCommandParser` + `MetaCommandService`) com os comandos `/clear`, `/help` e `/lang`. Os comandos que dependem de resolução de assemblies (`/reference`, `/import`, `/references`, `/imports`) e de timeout serão implementados nos blocos TASKS-9 e TASKS-10 respectivamente.

A persistência de sessão garante que referências, imports, timeout e linguagem sobrevivam ao fechamento da aplicação.

---

## Domain Model

### Class: `SessionSettings` (`src/QuickNET.Core/Models/SessionSettings.cs`)

```csharp
namespace QuickNET.Models;

public class SessionSettings
{
    public List<string> ExtraReferences { get; set; } = [];
    public List<string> ExtraImports { get; set; } = [];
    public int TimeoutSeconds { get; set; } = 30;
    public string Language { get; set; } = "CSharp";
}
```

### Class: `MetaCommandResult` (`src/QuickNET.Core/Models/MetaCommandResult.cs`)

```csharp
namespace QuickNET.Models;

public class MetaCommandResult
{
    public string Command { get; init; } = "";
    public string DisplayText { get; init; } = "";
    public bool Success { get; init; } = true;
}
```

---

## Tasks

### 8.1 Criar SessionSettings

Arquivo `src/QuickNET.Core/Models/SessionSettings.cs` conforme o domain model acima.

- `ExtraReferences`: lista de nomes de assemblies adicionados via `/reference`.
- `ExtraImports`: lista de namespaces adicionados via `/import` ou `/using`.
- `TimeoutSeconds`: timeout atual; `30` por padrão; `0` = sem limite.
- `Language`: `"CSharp"` ou `"VisualBasic"`.

### 8.2 Criar SessionState (singleton persistente)

Arquivo `src/QuickNET.Core/Session/SessionState.cs`:

```csharp
using System.Text.Json;
using QuickNET.Models;

namespace QuickNET.Session;

public class SessionState
{
    private readonly string _filePath;
    private SessionSettings _settings = new();

    public IReadOnlyList<string> ExtraReferences => _settings.ExtraReferences.AsReadOnly();
    public IReadOnlyList<string> ExtraImports => _settings.ExtraImports.AsReadOnly();

    public int TimeoutSeconds
    {
        get => _settings.TimeoutSeconds;
        set { _settings.TimeoutSeconds = value; Save(); }
    }

    public Language CurrentLanguage
    {
        get => Enum.TryParse<Language>(_settings.Language, out var lang) ? lang : Language.CSharp;
        set { _settings.Language = value.ToString(); Save(); }
    }

    public SessionState()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(appData, "QuickNET");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "settings.json");
        Load();
    }

    // Métodos de mutação — chamam Save() automaticamente
    public void AddReference(string assemblyName) { ... }
    public bool RemoveReference(string assemblyName) { ... }
    public void AddImport(string namespaceName) { ... }
    public bool RemoveImport(string namespaceName) { ... }

    private void Load() { ... }
    private void Save() { ... }
}
```

**Implementação de `Load()`:**
1. Se `File.Exists(_filePath)`, ler conteúdo com `File.ReadAllText`.
2. Desserializar com `JsonSerializer.Deserialize<SessionSettings>(json)`.
3. Se sucesso, atribuir a `_settings`.
4. Se qualquer exceção (arquivo corrompido, JSON inválido, acesso negado), manter `_settings` com defaults e logar via `Debug.WriteLine`. **Não crashar.**
5. Se o arquivo não existir, manter defaults (primeira execução).

**Implementação de `Save()`:**
1. Serializar `_settings` com `JsonSerializer.Serialize` usando:
   ```csharp
   var options = new JsonSerializerOptions
   {
       WriteIndented = true,
       PropertyNamingPolicy = JsonNamingPolicy.CamelCase
   };
   ```
2. Escrever com `File.WriteAllText(_filePath, json)`.
3. Capturar exceções de I/O e logar; não propagar.

**Implementação de `AddReference()`:**
- Verifica se o nome já existe na lista (case-insensitive). Se existir, ignora (sem duplicar) e retorna sem salvar.
- Adiciona à lista e chama `Save()`.

**Implementação de `RemoveReference()`:**
- Remove da lista (case-insensitive). Retorna `true` se removeu, `false` se não encontrou.
- Chama `Save()` se removeu.

**Implementação de `AddImport()` / `RemoveImport()`:**
- Análogo a AddReference/RemoveReference, mas para namespaces.

### 8.3 Criar MetaCommandParser

Arquivo `src/QuickNET.Core/MetaCommands/MetaCommandParser.cs`:

```csharp
namespace QuickNET.MetaCommands;

public static class MetaCommandParser
{
    public static bool IsMetaCommand(string input)
    {
        return !string.IsNullOrWhiteSpace(input) && input.TrimStart().StartsWith('/');
    }

    public static (string Command, string? Args) Parse(string input)
    {
        var trimmed = input.TrimStart();
        if (!trimmed.StartsWith('/'))
            return ("", null);

        var spaceIndex = trimmed.IndexOf(' ');
        if (spaceIndex < 0)
            return (trimmed[1..].ToLowerInvariant(), null);

        var command = trimmed[1..spaceIndex].ToLowerInvariant();
        var args = trimmed[(spaceIndex + 1)..].Trim();
        return (command, args.Length > 0 ? args : null);
    }
}
```

### 8.4 Criar MetaCommandService

Arquivo `src/QuickNET.Core/MetaCommands/MetaCommandService.cs`:

```csharp
using QuickNET.Models;
using QuickNET.Session;

namespace QuickNET.MetaCommands;

public class MetaCommandService
{
    private readonly SessionState _sessionState;

    public MetaCommandService(SessionState sessionState)
    {
        _sessionState = sessionState;
    }

    public MetaCommandResult Execute(string input)
    {
        var (command, args) = MetaCommandParser.Parse(input);

        if (string.IsNullOrEmpty(command))
            return new MetaCommandResult
            {
                Command = "",
                DisplayText = "Not a meta-command.",
                Success = false
            };

        return command switch
        {
            "clear" => ExecuteClear(),
            "help" => ExecuteHelp(),
            "lang" => ExecuteLang(args),
            // Demais comandos implementados no TASKS-9 e TASKS-10
            "reference" => NotYetImplemented(command),
            "import" or "using" => NotYetImplemented(command),
            "references" => NotYetImplemented(command),
            "imports" => NotYetImplemented(command),
            "timeout" => NotYetImplemented(command),
            _ => new MetaCommandResult
            {
                Command = command,
                DisplayText = $"Unknown command '/{command}'. Type /help for available commands.",
                Success = false
            }
        };
    }

    private MetaCommandResult ExecuteClear()
    {
        // O clear de ConversationItems é feito pelo ViewModel.
        // Aqui apenas sinalizamos que foi um comando clear.
        return new MetaCommandResult
        {
            Command = "clear",
            DisplayText = "Conversation cleared.",
            Success = true
        };
    }

    private MetaCommandResult ExecuteHelp()
    {
        var help = """
            Available commands:
              /clear                 Clear the conversation panel and history
              /help                  Show this help message
              /lang <cs|vb>          Switch language (cs = C#, vb = VB.NET)
              /reference <assembly>  Add an assembly reference
              /import <namespace>    Add a namespace import (alias: /using)
              /references            List all referenced assemblies
              /imports               List all imported namespaces
              /timeout <seconds>     Set execution timeout (0 = no limit)
            """;
        return new MetaCommandResult
        {
            Command = "help",
            DisplayText = help,
            Success = true
        };
    }

    private MetaCommandResult ExecuteLang(string? args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new MetaCommandResult
            {
                Command = "lang",
                DisplayText = $"Current language: {_sessionState.CurrentLanguage}. Usage: /lang <cs|vb>",
                Success = false
            };

        var lang = args.Trim().ToLowerInvariant();
        if (lang == "cs" || lang == "csharp" || lang == "c#")
        {
            _sessionState.CurrentLanguage = Language.CSharp;
            return new MetaCommandResult
            {
                Command = "lang",
                DisplayText = "Language set to C#.",
                Success = true
            };
        }

        if (lang == "vb" || lang == "vbnet" || lang == "vb.net" || lang == "visualbasic")
        {
            _sessionState.CurrentLanguage = Language.VisualBasic;
            return new MetaCommandResult
            {
                Command = "lang",
                DisplayText = "Language set to VB.NET.",
                Success = true
            };
        }

        return new MetaCommandResult
        {
            Command = "lang",
            DisplayText = $"Unknown language '{args}'. Use 'cs' for C# or 'vb' for VB.NET.",
            Success = false
        };
    }

    private static MetaCommandResult NotYetImplemented(string command)
    {
        return new MetaCommandResult
        {
            Command = command,
            DisplayText = $"Command '/{command}' will be available in the next task.",
            Success = false
        };
    }
}
```

### 8.5 Registrar no DI

Atualizar `src/QuickNET.Core/ServiceCollectionExtensions.cs`:

```csharp
using QuickNET.Session;
using QuickNET.MetaCommands;

// Dentro de AddQuickNETCore:
services.AddSingleton<SessionState>();
services.AddSingleton<MetaCommandService>();
```

### 8.6 Atualizar MainWindowViewModel para rotear meta-comandos

No `MainWindowViewModel.ExecuteCode()`, adicionar verificação de meta-comando antes do pipeline de compilação:

```csharp
[RelayCommand]
private void ExecuteCode()
{
    if (string.IsNullOrWhiteSpace(InputText)) return;

    // Check for meta-command
    if (MetaCommandParser.IsMetaCommand(InputText))
    {
        ExecuteMetaCommand(InputText);
        return;
    }

    // ... existing compilation/execution logic ...
}

private void ExecuteMetaCommand(string input)
{
    var result = _metaCommandService.Execute(input);

    // Add input to conversation
    ConversationItems.Add(new ConversationItem
    {
        DisplayText = $"> {input.TrimEnd()}",
        IsInput = true
    });

    // Handle clear side-effect
    if (result.Command == "clear")
    {
        ConversationItems.Clear();
        _history.Clear();
    }

    // Handle lang side-effect: sync ComboBox
    if (result.Command == "lang" && result.Success)
    {
        SelectedLanguageIndex = _sessionState.CurrentLanguage == Language.CSharp ? 0 : 1;
    }

    // Add output to conversation
    ConversationItems.Add(new ConversationItem
    {
        DisplayText = result.DisplayText,
        IsInput = false,
        IsError = !result.Success
    });

    StatusText = result.Success ? "Ready" : "Error";
    InputText = "";
}
```

Para isso, injetar `MetaCommandService` e `SessionState` no construtor do `MainWindowViewModel`.

### 8.7 Carregar linguagem do SessionState na inicialização

No construtor do `MainWindowViewModel`, após `LoadHistory()`, sincronizar o ComboBox com o `SessionState`:

```csharp
public MainWindowViewModel(ReplEngine engine, HistoryService history,
                           MetaCommandService metaCommandService, SessionState sessionState)
{
    _engine = engine;
    _history = history;
    _metaCommandService = metaCommandService;
    _sessionState = sessionState;
    LoadHistory();

    // Restore saved language
    _selectedLanguageIndex = _sessionState.CurrentLanguage == Language.CSharp ? 0 : 1;
}
```

### 8.8 Atualizar DI do Program.cs

Registrar novas dependências (se necessário — `MetaCommandService` e `SessionState` já estão registrados via `AddQuickNETCore()`).

**Verificar:** O `MainWindowViewModel` agora recebe 2 parâmetros extras no construtor. O DI do `Microsoft.Extensions.DependencyInjection` resolve isso automaticamente desde que todos os tipos estejam registrados.

---

## Acceptance Criteria

- [ ] `SessionState` criado pela primeira vez gera `settings.json` com os defaults (ExtraReferences=[], ExtraImports=[], TimeoutSeconds=30, Language="CSharp").
- [ ] Alterar `CurrentLanguage` persiste no disco imediatamente.
- [ ] Reiniciar a aplicação restaura a linguagem selecionada anteriormente.
- [ ] `MetaCommandParser.IsMetaCommand("/anything")` retorna `true`.
- [ ] `MetaCommandParser.IsMetaCommand("2 + 2")` retorna `false`.
- [ ] `MetaCommandParser.Parse("/lang vb")` retorna `("lang", "vb")`.
- [ ] `MetaCommandParser.Parse("/help")` retorna `("help", null)`.
- [ ] Digitar `/help` e pressionar Enter exibe a lista de comandos no painel de conversação.
- [ ] Digitar `/lang vb` altera a linguagem para VB.NET e atualiza o ComboBox.
- [ ] Digitar `/clear` limpa o painel de conversação e o histórico persistido.
- [ ] Digitar `/xyz` (comando inexistente) exibe `Unknown command '/xyz'. Type /help for available commands.`
- [ ] `settings.json` corrompido não causa crash — inicia com defaults.
- [ ] Adicionar referências duplicadas (`/reference System.Text.Json` duas vezes) não duplica a entrada.

---

## Notes for AI Agent

- O `SessionState` é um singleton — todas as mutações são imediatamente persistidas via `Save()`.
- O `MetaCommandService` **não** depende de `HistoryService` diretamente — o clear do histórico é feito pelo ViewModel ao detectar `result.Command == "clear"`. Isso evita acoplamento bidirecional entre Core e App.
- O `MetaCommandService` recebe `SessionState` via DI. Os comandos `/reference`, `/import` e `/timeout` que precisam de serviços adicionais terão seus handlers completados nos TASKS-9 e TASKS-10.
- Os comandos não implementados ainda (`/reference`, `/import`, `/references`, `/imports`, `/timeout`) retornam `NotYetImplemented` com mensagem informativa. Eles serão implementados nos próximos blocos.
- O namespace `QuickNET.MetaCommands` é novo. Criar o diretório `src/QuickNET.Core/MetaCommands/`.
- O namespace `QuickNET.Session` é novo. Criar o diretório `src/QuickNET.Core/Session/`.
- Para o `using` no ViewModel: adicionar `using QuickNET.MetaCommands;` e `using QuickNET.Session;`.
- A sincronização bidirecional entre `/lang` e o ComboBox:
  - `/lang vb` → `SelectedLanguageIndex = 1` (ViewModel atualiza ComboBox)
  - ComboBox muda → `SelectedLanguageIndex` property setter deve também atualizar `_sessionState.CurrentLanguage` (adicionar no setter ou via `partial void OnSelectedLanguageIndexChanged`)
