# TASKS-4: History Persistence

**Block:** 4 de 7
**Depends on:** TASKS-2 (model types definidos)
**PRD Reference:** `docs/PRD.md` — Seções 2.2 (US-04), 5.3

---

## Objective

Implementar o gerenciador de histórico que persiste entradas de execução em JSON no diretório `%APPDATA%\QuickNET\history.json` e as expõe como coleção observável para binding na UI.

---

## Domain Model

### Record: `HistoryEntry` (`src/QuickNET.Core/Models/HistoryEntry.cs`)

```csharp
using System.Text.Json.Serialization;

namespace QuickNET.Models;

public record HistoryEntry
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.Now;

    [JsonPropertyName("language")]
    public string Language { get; init; } = "";  // "CSharp" ou "VisualBasic"

    [JsonPropertyName("input")]
    public string Input { get; init; } = "";

    [JsonPropertyName("output")]
    public string Output { get; init; } = "";

    [JsonPropertyName("isError")]
    public bool IsError { get; init; }
}
```

---

## Tasks

### 4.1 Criar HistoryManager

Classe em `src/QuickNET.Core/History/HistoryManager.cs`:

```csharp
using System.Text.Json;
using QuickNET.Models;

namespace QuickNET.History;

public class HistoryManager
{
    private readonly string _filePath;
    private readonly int _maxEntries;
    private List<HistoryEntry> _entries = [];

    public IReadOnlyList<HistoryEntry> Entries => _entries.AsReadOnly();

    public HistoryManager(int maxEntries = 500)
    {
        _maxEntries = maxEntries;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var directory = Path.Combine(appData, "QuickNET");
        Directory.CreateDirectory(directory);
        _filePath = Path.Combine(directory, "history.json");
        Load();
    }

    public void AddEntry(HistoryEntry entry)
    {
        _entries.Add(entry);
        while (_entries.Count > _maxEntries)
            _entries.RemoveAt(0);
        Save();
    }

    public void Clear()
    {
        _entries.Clear();
        Save();
    }

    private void Load() { ... }
    private void Save() { ... }
}
```

Implementação de `Load()`:
1. Se `File.Exists(_filePath)`, ler todo o conteúdo com `File.ReadAllText`.
2. Desserializar com `JsonSerializer.Deserialize<List<HistoryEntry>>(json)`.
3. Se o JSON for inválido ou o arquivo estiver corrompido, iniciar com lista vazia (não crashar).

Implementação de `Save()`:
1. Serializar `_entries` com `JsonSerializer.Serialize` usando opções `WriteIndented = true`.
2. Escrever com `File.WriteAllText`.

### 4.2 Criar HistoryService (wrapper thread-safe)

Classe em `src/QuickNET.Core/History/HistoryService.cs`:

```csharp
using QuickNET.Models;

namespace QuickNET.History;

public class HistoryService
{
    private readonly HistoryManager _manager;

    public HistoryService(HistoryManager manager)
    {
        _manager = manager;
    }

    public void Record(string input, string language, string output, bool isError)
    {
        _manager.AddEntry(new HistoryEntry
        {
            Timestamp = DateTime.Now,
            Language = language,
            Input = input,
            Output = output,
            IsError = isError
        });
    }

    public IReadOnlyList<HistoryEntry> GetEntries() => _manager.Entries;
    public void Clear() => _manager.Clear();
}
```

### 4.3 Registrar no DI

Em `src/QuickNET.Core/ServiceCollectionExtensions.cs`, adicionar:

```csharp
using QuickNET.History;

// Dentro de AddQuickNETCore:
services.AddSingleton<HistoryManager>();
services.AddSingleton<HistoryService>();
```

---

## Acceptance Criteria

- [ ] `HistoryManager` criado pela primeira vez cria o diretório `%APPDATA%\QuickNET\` e arquivo `history.json` vazio.
- [ ] `AddEntry` adiciona a entrada e persiste no disco imediatamente.
- [ ] `Load` carrega entradas previamente salvas entre reinicializações da aplicação.
- [ ] Arquivo JSON corrompido não causa crash — `Load` retorna lista vazia.
- [ ] Limite de 500 entradas é respeitado (entrada mais antiga removida ao exceder).
- [ ] `Clear` remove todas as entradas da memória e do disco.

---

## Notes for AI Agent

- O path `%APPDATA%` em Windows resolve para algo como `C:\Users\<user>\AppData\Roaming`. Usar `Environment.SpecialFolder.ApplicationData` é a forma correta de obter esse caminho.
- Para `JsonSerializerOptions`, usar:

```csharp
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
```

- O `HistoryService` existe como fachada simples para que a UI não dependa diretamente do `HistoryManager` e seus detalhes de serialização.
- O `HistoryManager` não precisa ser thread-safe no MVP (a UI do Avalonia é single-threaded por padrão).
- O método `Load` deve ser chamado no construtor. Se houver qualquer exceção durante `Load` (arquivo bloqueado, JSON malformado, etc.), logar no `Debug.WriteLine` e seguir com lista vazia.
