# TASKS-12: Tests for v1.1

**Block:** 12 de 12
**Depends on:** TASKS-8, TASKS-9, TASKS-10, TASKS-11
**PRD Reference:** `docs/PRD.md` — Seção 6 (Test Plan)

---

## Objective

Implementar testes unitários para todas as novas funcionalidades da v1.1: parser de meta-comandos, estado de sessão com persistência, serviço de meta-comandos, resolução de assemblies, timeout de execução, referências/imports dinâmicos, e ViewModel com roteamento de meta-comandos. Usar MSTest com as convenções do projeto.

---

## Test File Organization

```
tests/QuickNET.Tests/
  MetaCommands/
    MetaCommandParserTests.cs
    MetaCommandServiceTests.cs
  Session/
    SessionStateTests.cs
  Compilation/
    AssemblyResolutionServiceTests.cs
    DynamicCompilationTests.cs
  Execution/
    TimeoutTests.cs
  ViewModels/
    MainWindowViewModelV2Tests.cs
```

Os diretórios `MetaCommands/` e `Session/` devem ser criados.

---

## Tasks

### 12.1 MetaCommandParserTests

Arquivo `tests/QuickNET.Tests/MetaCommands/MetaCommandParserTests.cs`:

Classe: `[TestClass] sealed` com construtor vazio (sem dependências).

| Método | Descrição |
|---|---|
| `IsMetaCommand_PrefixSlash_ReturnsTrue` | `/help` → true |
| `IsMetaCommand_PrefixSlashWithSpaces_ReturnsTrue` | `  /help` → true (espaços antes) |
| `IsMetaCommand_PlainCode_ReturnsFalse` | `2 + 2` → false |
| `IsMetaCommand_EmptyString_ReturnsFalse` | `""` → false |
| `IsMetaCommand_NullOrWhitespace_ReturnsFalse` | `"   "` → false |
| `Parse_CommandOnly_ReturnsCommandNoArgs` | `/help` → `("help", null)` |
| `Parse_CommandWithArgs_ReturnsCommandAndArgs` | `/lang vb` → `("lang", "vb")` |
| `Parse_CommandWithMultipleArgs_ReturnsAllArgs` | `/timeout 60 seconds` → `("timeout", "60 seconds")` |
| `Parse_ArgsAreTrimmed_ReturnsTrimmedArgs` | `/import   System.Text.Json  ` → `("import", "System.Text.Json")` |
| `Parse_CommandIsLowercased_ReturnsLowercase` | `/HELP` → `("help", null)` |
| `Parse_NotMetaCommand_ReturnsEmpty` | `2 + 2` → `("", null)` |

### 12.2 SessionStateTests

Arquivo `tests/QuickNET.Tests/Session/SessionStateTests.cs`:

Classe: `[TestClass] sealed`. Usar diretório temporário (`Path.GetTempPath()`) para isolar testes de arquivos reais. Injetar path customizado no `SessionState` via construtor sobrecarregado (adicionar `internal SessionState(string filePath)` para testes).

| Método | Descrição |
|---|---|
| `Constructor_FirstRun_CreatesSettingsFile` | Criar SessionState com path temporário → arquivo existe com defaults (TimeoutSeconds=30, Language="CSharp") |
| `Constructor_ExistingFile_LoadsSettings` | Criar arquivo previamente com `TimeoutSeconds=60, Language="VisualBasic"` → SessionState carrega valores corretos |
| `Constructor_CorruptFile_FallsBackToDefaults` | Criar arquivo com JSON inválido → SessionState não crasha, usa defaults |
| `CurrentLanguage_Setter_PersistsImmediately` | Set `CurrentLanguage = VisualBasic` → ler arquivo do disco, contém `"language": "VisualBasic"` |
| `TimeoutSeconds_Setter_PersistsImmediately` | Set `TimeoutSeconds = 10` → ler arquivo, contém `"timeoutSeconds": 10` |
| `AddReference_NewReference_AddsAndPersists` | `AddReference("System.Text.Json")` → `ExtraReferences` contém "System.Text.Json", arquivo atualizado |
| `AddReference_DuplicateReference_Ignored` | `AddReference("System.Text.Json")` duas vezes → apenas 1 entrada |
| `RemoveReference_Existing_RemovesAndReturnsTrue` | `AddReference("A")`, `RemoveReference("A")` → retorna true, lista vazia |
| `RemoveReference_NonExisting_ReturnsFalse` | `RemoveReference("B")` → retorna false |
| `AddImport_NewNamespace_AddsAndPersists` | `AddImport("System.Text.Json")` → `ExtraImports` contém, arquivo atualizado |
| `AddImport_DuplicateNamespace_Ignored` | Duas chamadas → apenas 1 entrada |
| `ExtraReferences_IsReadOnly` | `ExtraReferences` é `IReadOnlyList<string>` (verificação de tipo) |

**Nota:** Para viabilizar os testes com path customizado, adicionar um construtor `internal` ao `SessionState`:

```csharp
internal SessionState(string filePath)
{
    _filePath = filePath;
    var directory = Path.GetDirectoryName(filePath)!;
    Directory.CreateDirectory(directory);
    Load();
}
```

E usar `[InternalsVisibleTo("QuickNET.Tests")]` no projeto Core ou via `AssemblyInfo.cs`.

### 12.3 MetaCommandServiceTests

Arquivo `tests/QuickNET.Tests/MetaCommands/MetaCommandServiceTests.cs`:

Classe: `[TestClass] sealed`. No construtor, criar `SessionState` com path temporário e injetar no `MetaCommandService`.

**Nota:** `/reference` e `/import` precisam de `AssemblyResolutionService`. Para os testes destes comandos específicos, criar o serviço completo. Para os demais, o `AssemblyResolutionService` pode ser um singleton real (sem mock).

| Método | Descrição |
|---|---|
| `Execute_Help_ReturnsCommandList` | `/help` → `Success == true`, `DisplayText` contém "Available commands" e todos os 8 comandos |
| `Execute_Clear_ReturnsSuccess` | `/clear` → `Success == true`, `Command == "clear"`, `DisplayText` contém "cleared" |
| `Execute_Lang_CSharp_SetsLanguage` | `/lang cs` → `Success == true`, `SessionState.CurrentLanguage == CSharp` |
| `Execute_Lang_VbNet_SetsLanguage` | `/lang vb` → `Success == true`, `SessionState.CurrentLanguage == VisualBasic` |
| `Execute_Lang_CSharpFull_SetsLanguage` | `/lang csharp` → reconhece como C# |
| `Execute_Lang_VbNetFull_SetsLanguage` | `/lang vbnet` → reconhece como VB.NET |
| `Execute_Lang_NoArgs_ShowsCurrent` | `/lang` → exibe linguagem atual |
| `Execute_Lang_InvalidArg_ReturnsError` | `/lang python` → `Success == false`, mensagem de erro |
| `Execute_Reference_ValidAssembly_AddsReference` | `/reference System.Text.Json` → `Success == true`, `SessionState.ExtraReferences` contém "System.Text.Json" |
| `Execute_Reference_InvalidAssembly_ReturnsError` | `/reference NonExistent.Assembly.Foo` → `Success == false`, contém "not found" |
| `Execute_Reference_NoArgs_ShowsUsage` | `/reference` sem args → `Success == false`, contém "Usage" |
| `Execute_Import_ValidNamespace_AddsImport` | `/import System.Text.Json` → `Success == true`, `SessionState.ExtraImports` contém |
| `Execute_Import_NoArgs_ShowsUsage` | `/import` sem args → `Success == false`, contém "Usage" |
| `Execute_Using_Alias_AddsImport` | `/using System.Text.Json` → funciona igual `/import` |
| `Execute_References_Empty_ShowsDefaults` | `/references` → `DisplayText` menciona "System.Runtime" e "No extra references" |
| `Execute_References_WithExtras_ListsBoth` | Adicionar `/reference System.Text.Json`, depois `/references` → exibe defaults + "System.Text.Json" |
| `Execute_Imports_Empty_ShowsDefaults` | `/imports` → mostra imports padrão |
| `Execute_Imports_WithExtras_ListsBoth` | Adicionar `/import System.Text.Json`, depois `/imports` → exibe defaults + extra |
| `Execute_Timeout_ValidValue_SetsTimeout` | `/timeout 60` → `Success == true`, `SessionState.TimeoutSeconds == 60` |
| `Execute_Timeout_Zero_SetsNoLimit` | `/timeout 0` → `Success == true`, `TimeoutSeconds == 0` |
| `Execute_Timeout_NoArgs_ShowsCurrent` | `/timeout` → exibe timeout atual |
| `Execute_Timeout_Negative_ReturnsError` | `/timeout -5` → `Success == false`, contém "Invalid" |
| `Execute_Timeout_NonNumeric_ReturnsError` | `/timeout abc` → `Success == false`, contém "Invalid" |
| `Execute_UnknownCommand_ReturnsError` | `/xyz` → `Success == false`, contém "Unknown command" e sugere `/help` |
| `Execute_EmptyInput_ReturnsError` | `""` → `Success == false`, `Command == ""` |

### 12.4 AssemblyResolutionServiceTests

Arquivo `tests/QuickNET.Tests/Compilation/AssemblyResolutionServiceTests.cs`:

Classe: `[TestClass] sealed`.

| Método | Descrição |
|---|---|
| `Resolve_ValidSystemAssembly_ReturnsMetadataReference` | `"System.Text.Json"` → retorna não-nulo, `LoadedAssemblyNames` contém |
| `Resolve_ValidCoreLib_ReturnsMetadataReference` | `"System.Runtime"` → retorna não-nulo |
| `Resolve_NonExistentAssembly_ReturnsNull` | `"NonExistent.Fake.Assembly"` → retorna null |
| `Resolve_SameNameTwice_UsesCache` | Resolver `"System.Text.Json"` duas vezes → `LoadedAssemblyNames.Count` permanece estável (não incrementa duplicado) |
| `GetAllReferences_AfterResolutions_ReturnsList` | Resolver 2 assemblies → `GetAllReferences().Count >= 2` |

**Nota:** `Assembly.Load("System.Text.Json")` pode não encontrar o assembly se ele não estiver no contexto default. Se falhar no ambiente de teste, usar assemblies conhecidos como `"System.Runtime"` ou `"System.Linq"` que sempre estão disponíveis. Adaptar os testes conforme necessário.

### 12.5 TimeoutTests

Arquivo `tests/QuickNET.Tests/Execution/TimeoutTests.cs`:

Classe: `[TestClass] sealed`. Depende de `CompilationService` e `ExecutionService` (usar DI real — as dependências são leves).

| Método | Descrição |
|---|---|
| `Execute_WithAdequateTimeout_ReturnsSuccess` | Compilar e executar `2 + 2` com `timeoutSeconds = 30` → `Success == true`, `Output == "4"` |
| `Execute_WithZeroTimeout_SkipsTimeout` | `timeoutSeconds = 0` → mesmo comportamento do original, `Success == true` |
| `Execute_WithExpiredTimeout_ReturnsTimeoutError` | Compilar/executar `System.Threading.Thread.Sleep(2000); return 42;` com `timeoutSeconds = 1` → `Success == false`, `Error` contém "timed out after 1" |
| `Execute_Timeout_StillCapturesConsoleOutput` | Código com `Console.WriteLine("before"); Thread.Sleep(5000);` + `timeoutSeconds = 1` → `ConsoleOutput` contém "before" mesmo com timeout |
| `Execute_Timeout_UnloadsALC` | Executar código com timeout, verificar que ALC foi descartado (executar 10x com timeout, processo não cresce em memória — verificação básica de smoke) |

### 12.6 DynamicCompilationTests

Arquivo `tests/QuickNET.Tests/Compilation/DynamicCompilationTests.cs`:

Classe: `[TestClass] sealed`. Testes de integração entre `CompilationService` e referências/imports extras.

| Método | Descrição |
|---|---|
| `Compile_WithExtraReference_UsesAssembly` | `CompilationInput` com `ExtraReferences = ["System.Text.Json"]` — compilar código que usa `JsonSerializer.Serialize(42)` → `Success == true` |
| `Compile_WithExtraImport_UsesNamespace` | `CompilationInput` com `ExtraImports = ["System.Text.Json"]` — compilar código com `JsonSerializer.Serialize(42)` sem qualificação → `Success == true` |
| `Compile_WithExtraImport_LineOffsetCorrect` | Código com erro na linha 1 do usuário, 2 imports extras → diagnostic tem `Line == 1` (offset ajustado) |
| `Compile_WithoutExtraReference_UsesDefaultOnly` | Sem extra references — compilar `typeof(File)` funciona (System.IO padrão) |
| `Compile_ExtraImport_InjectedIntoGeneratedCode` | Verificar que o código gerado por `CSharpTemplateEngine.GenerateCode("...", ["System.Text.Json"])` contém `using System.Text.Json;` após os usings padrão |
| `Compile_VbNet_ExtraImport_InjectedIntoGeneratedCode` | Mesmo para VB.NET: `VbTemplateEngine.GenerateCode("...", ["System.Text.Json"])` contém `Imports System.Text.Json` |

### 12.7 MainWindowViewModelV2Tests

Arquivo `tests/QuickNET.Tests/ViewModels/MainWindowViewModelV2Tests.cs`:

Classe: `[TestClass] sealed`. Construtor injeta `ReplEngine`, `HistoryService`, `MetaCommandService`, `SessionState` reais ou com stubs leves.

**Nota:** Para não depender do `ReplEngine` real (que compila e executa), considerar criar uma interface `IReplEngine` ou usar um `ReplEngine` real que é rápido o suficiente. Alternativa: mockar `ReplEngine` para testes de ViewModel puro.

| Método | Descrição |
|---|---|
| `ExecuteCode_MetaCommand_CallsMetaService` | `InputText = "/help"`, chamar `ExecuteCodeCommand.Execute()` → `ConversationItems` contém output do help, **não** tenta compilar |
| `ExecuteCode_MetaCommand_Clear_ClearsPanel` | Adicionar items prévios, `InputText = "/clear"`, executar → `ConversationItems` vazio |
| `ExecuteCode_MetaCommand_Lang_SyncsComboBox` | `InputText = "/lang vb"`, executar → `SelectedLanguageIndex == 1` |
| `ExecuteCode_MetaCommand_Timeout_SyncsComboBox` | `InputText = "/timeout 5"`, executar → `SelectedTimeoutIndex == 0` (índice de 5s) |
| `ExecuteCode_MetaCommand_Unknown_ShowsError` | `InputText = "/xyz"`, executar → último `ConversationItem.IsError == true` |
| `ExecuteCode_MetaCommand_EmptyInput_NoOp` | `InputText = ""` ou `"  "`, executar → `ConversationItems.Count` não muda |
| `RestoreSessionSettings_RestoresLanguage` | Criar ViewModel com `SessionState.CurrentLanguage = VisualBasic` → `SelectedLanguageIndex == 1` |
| `RestoreSessionSettings_RestoresTimeout` | Criar ViewModel com `SessionState.TimeoutSeconds = 10` → `SelectedTimeoutIndex == 1` (índice de 10s) |
| `ComboBoxLanguage_SyncsToSessionState` | Alterar `SelectedLanguageIndex = 1` → `SessionState.CurrentLanguage == VisualBasic` |
| `ComboBoxTimeout_SyncsToSessionState` | Alterar `SelectedTimeoutIndex = 0` (5s) → `SessionState.TimeoutSeconds == 5` |
| `StatusText_AfterSuccessfulMetaCommand_IsReady` | Executar `/help` → `StatusText == "Ready"` |
| `StatusText_AfterFailedMetaCommand_IsError` | Executar `/xyz` → `StatusText == "Error"` |

---

## Acceptance Criteria

- [ ] `dotnet test` executa **todos** os testes (existentes + novos) sem falhas.
- [ ] Cobertura de código nos novos módulos >= 70% (`MetaCommands`, `Session`, `AssemblyResolutionService`, timeout, dynamic compilation).
- [ ] Testes de `SessionState` usam diretório temporário (não poluem `%APPDATA%` real).
- [ ] Testes de `MetaCommandParser` cobrem todos os cenários (com/sem args, case-insensitivity, não-meta-command).
- [ ] Testes de `MetaCommandService` cobrem todos os 8 comandos com cenários de sucesso e erro.
- [ ] Testes de timeout verificam tanto o caso de sucesso (timeout adequado) quanto expirado.
- [ ] Testes de `DynamicCompilation` verificam que referências extras realmente permitem usar APIs adicionais.
- [ ] Linha/coluna nos diagnósticos permanece correta após adição de imports extras.

---

## Notes for AI Agent

- **InternalsVisibleTo:** Adicionar `[assembly: InternalsVisibleTo("QuickNET.Tests")]` no `QuickNET.Core` (via `AssemblyInfo.cs` ou inline no csproj) para que o construtor `internal SessionState(string filePath)` seja acessível nos testes.
  ```xml
  <!-- QuickNET.Core.csproj -->
  <ItemGroup>
    <InternalsVisibleTo Include="QuickNET.Tests" />
  </ItemGroup>
  ```
- **Diretório temporário para SessionState:** Usar `Path.Combine(Path.GetTempPath(), "QuickNET_Tests", Guid.NewGuid().ToString())` e limpar no `[TestCleanup]`.
- **Mocking de ReplEngine:** Se os testes de ViewModel forem muito lentos com `ReplEngine` real, considerar extrair uma interface `IReplEngine` do `ReplEngine` e mockar nos testes de ViewModel. Isso é opcional — o `ReplEngine` real é rápido para expressões simples.
- **AssemblyResolutionService:** Manter como classe concreta nos testes (não mockar). Resolver assemblies reais é importante para validar o comportamento.
- **Ordem de dependência nos testes:** `TimeoutTests` e `DynamicCompilationTests` dependem de `CompilationService` + `ExecutionService` reais. Instanciá-los via construtor ou criar helpers de factory.
- **Testes de arquivo:** `SessionStateTests` que escrevem em disco devem usar `[TestCleanup]` para deletar o diretório temporário após cada teste.
- **Convenção de nomes:** Seguir `MethodName_Scenario_ExpectedBehavior` em todos os testes.
- **Classes sealed:** Todos os `[TestClass]` devem ser `sealed`.
- **Construtor vs TestInitialize:** Preferir construtor para setup. Usar `[TestCleanup]` apenas para cleanup de recursos externos (arquivos temporários).
- **Paralelismo:** O assembly já tem `[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]`. Testes de arquivo (`SessionStateTests`) devem usar paths únicos por teste para evitar conflitos de paralelismo.
