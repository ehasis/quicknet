# TASKS-7: Unit Tests

**Block:** 7 de 7
**Depends on:** TASKS-2, TASKS-3, TASKS-4, TASKS-6 (todos os anteriores)
**PRD Reference:** `docs/PRD.md` — Seção 6

---

## Objective

Implementar testes unitários com cobertura >= 70% nos módulos core (compilação, execução, templates, histórico) e testes de ViewModel. Usar MSTest.

---

## Tasks

### 7.1 Template Engine Tests

Arquivo: `tests/QuickNET.Tests/Compilation/TemplateEngineTests.cs`

| Teste | Descrição |
|---|---|
| `CSharp_SimpleExpression_WrapsWithReturn` | Input `2 + 2` gera código com `return 2 + 2;` |
| `CSharp_Statement_NoWrapping` | Input `var x = 10;` não adiciona `return` |
| `CSharp_MultiLine_Block_NoWrapping` | Input multi-linha com `if`, `for`, etc. mantém estrutura original |
| `CSharp_UsesDefaultNamespaces` | Código gerado contém `using System;`, `using System.IO;`, etc. |
| `VbNet_SimpleExpression_WrapsWithReturn` | Input `2 + 2` gera `Return 2 + 2` |
| `VbNet_Statement_NoWrapping` | Input `Dim x = 10` não adiciona `Return` |
| `VbNet_UsesDefaultImports` | Código gerado contém `Imports System`, `Imports System.IO`, etc. |
| `SupportedLanguage_Returns_CorrectEnum` | `CSharpTemplateEngine.SupportedLanguage == Language.CSharp` e `VbTemplateEngine.SupportedLanguage == Language.VisualBasic` |

### 7.2 CompilationService Tests

Arquivo: `tests/QuickNET.Tests/Compilation/CompilationServiceTests.cs`

| Teste | Descrição |
|---|---|
| `Compile_ValidCSharp_ReturnsSuccess` | `2 + 2` → `Success == true`, `AssemblyBytes` não nulo |
| `Compile_ValidVbNet_ReturnsSuccess` | `2 + 2` → `Success == true` |
| `Compile_InvalidCode_ReturnsError` | `2 +` → `Success == false`, `Diagnostics` contém erro |
| `Compile_InvalidCode_DiagnosticsHaveLineInfo` | Erro de compilação tem `Line` e `Column` ajustados ao input original |
| `Compile_ComplexExpression_ReturnsSuccess` | `Enumerable.Range(1,5).Sum()` → sucesso |
| `Compile_MultiLine_CSharp_ReturnsSuccess` | Bloco com `var x = 10; var y = 20; return x + y;` → sucesso |
| `Compile_ConsoleWriteLine_ReturnsSuccess` | `Console.WriteLine("test");` → sucesso |

### 7.3 ExecutionService Tests

Arquivo: `tests/QuickNET.Tests/Execution/ExecutionServiceTests.cs`

**Nota:** Estes testes precisam de um `CompilationService` para gerar os bytes do assembly primeiro (teste de integração leve).

| Teste | Descrição |
|---|---|
| `Execute_SimpleMath_ReturnsResult` | Compilar e executar `2 + 2` → `Output == "4"` |
| `Execute_StringExpression_ReturnsResult` | `"hello" + " world"` → `Output == "hello world"` |
| `Execute_ConsoleWriteLine_CapturesOutput` | `Console.WriteLine("captured");` → `ConsoleOutput` contém `"captured"` |
| `Execute_RuntimeException_ReturnsError` | `throw new Exception("fail");` → `Success == false`, `Error` contém `"fail"` |
| `Execute_ReturnsTask_UnwrapsResult` | `Task.FromResult(42)` → `Output == "42"` (não o nome do tipo Task) |
| `Execute_MultipleExecutions_NoMemoryLeak` | Executar 20x seguidas e verificar que não crasha (teste básico de smoke) |

### 7.4 ReplEngine Tests

Arquivo: `tests/QuickNET.Tests/Execution/ReplEngineTests.cs`

| Teste | Descrição |
|---|---|
| `Execute_CSharp_PipelineComplete` | Pipeline completo: `2 + 2` C# → `Success == true`, `Output == "4"` |
| `Execute_VbNet_PipelineComplete` | Pipeline completo: `2 + 2` VB.NET → `Success == true`, `Output == "4"` |
| `Execute_CompilationError_ReturnsFormattedError` | `2 +` C# → `Success == false`, `Error` contém mensagem formatada |
| `Execute_BothLanguages_Alternate` | Executar C# depois VB.NET, ambos funcionam corretamente |

### 7.5 HistoryManager Tests

Arquivo: `tests/QuickNET.Tests/History/HistoryManagerTests.cs`

| Teste | Descrição |
|---|---|
| `AddEntry_IncreasesCount` | Adicionar 1 entry → `Entries.Count == 1` |
| `AddEntry_PersistsToDisk` | Adicionar entry, criar novo `HistoryManager` → entry carregada |
| `MaxEntries_EvictsOldest` | Adicionar 501 entries → `Entries.Count == 500`, a mais antiga removida |
| `Clear_RemovesAllEntries` | Adicionar 3 entries, Clear → `Entries.Count == 0` |
| `Load_CorruptedFile_ReturnsEmpty` | Criar `history.json` com conteúdo inválido → não crasha, lista vazia |
| `Load_MissingFile_ReturnsEmpty` | Deletar `history.json` → novo `HistoryManager` começa vazio |

### 7.6 ViewModel Tests

Arquivo: `tests/QuickNET.Tests/ViewModels/MainWindowViewModelTests.cs`

Estes testes precisam de um `ReplEngine` real (ou mock). Para MVP, usar o engine real é aceitável (teste de integração via ViewModel).

| Teste | Descrição |
|---|---|
| `ExecuteCode_ValidInput_AddsConversationItems` | `InputText = "2 + 2"`, executar → `ConversationItems.Count >= 2` |
| `ExecuteCode_InvalidInput_AddsErrorItem` | `InputText = "2 +"`, executar → último item tem `IsError == true` |
| `ExecuteCode_EmptyInput_NoOp` | `InputText = ""`, executar → `ConversationItems.Count` não muda |
| `ClearHistory_RemovesAllItems` | Adicionar items, ClearHistory → `ConversationItems.Count == 0` |
| `SelectedLanguageIndex_DefaultIsCSharp` | `SelectedLanguageIndex == 0` (C#) |
| `LanguageSwitch_VbNet_ExecutesCorrectly` | `SelectedLanguageIndex = 1`, `InputText = "2 + 2"` → executa em VB.NET |

---

## Acceptance Criteria

- [ ] `dotnet test` executa todos os testes, com 0 falhas.
- [ ] Cobertura de código >= 70% nos módulos `QuickNET.Core`.
- [ ] Cada método de teste tem o atributo `[TestClass]` na classe e `[TestMethod]` em cada teste.
- [ ] Testes de HistoryManager usam um diretório temporário (`Path.GetTempPath() + Guid`) para não poluir o `%APPDATA%`.

---

## Notes for AI Agent

- Usar `[TestInitialize]` para setup comum (ex.: criar `CompilationService`, `ExecutionService`, limpar diretório de histórico temporário).
- Usar `[TestCleanup]` para limpar recursos (ex.: deletar diretório temporário de histórico após cada teste).
- Para `HistoryManager`, o construtor salva em `%APPDATA%` por padrão. Nos testes, injetar um path customizado. Adicionar um overload de construtor:

```csharp
// Em HistoryManager, adicionar:
public HistoryManager(string filePath, int maxEntries = 500)
{
    _maxEntries = maxEntries;
    _filePath = filePath;
    var directory = Path.GetDirectoryName(filePath);
    if (directory != null) Directory.CreateDirectory(directory);
    Load();
}
```

- Para o teste de Task unwrapping no ExecutionService, o código de input deve ser algo como:

C#:
```csharp
System.Threading.Tasks.Task.FromResult(42)
```

- Para VB.NET:
```vb
System.Threading.Tasks.Task.FromResult(42)
```

- O `dotnet test` deve ser executado com `--collect:"XPlat Code Coverage"` para gerar relatório de cobertura (opcional, mas desejável).

- **Boilerplate de classe de teste MSTest:**

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using QuickNET.Compilation;
using QuickNET.Models;

namespace QuickNET.Tests.Compilation;

[TestClass]
public class CompilationServiceTests
{
    private CompilationService _service = null!;

    [TestInitialize]
    public void Setup()
    {
        _service = new CompilationService();
    }

    [TestMethod]
    public void Compile_ValidCSharp_ReturnsSuccess()
    {
        var input = new CompilationInput("2 + 2", Language.CSharp);
        var result = _service.Compile(input);
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.AssemblyBytes);
    }
}
```

- Os assemblies `Microsoft.CodeAnalysis.CSharp` e `Microsoft.CodeAnalysis.VisualBasic` são pesados. A primeira execução de teste pode demorar alguns segundos para carregar. Isso é normal.
