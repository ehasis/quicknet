# TASKS-2: Core Compilation Engine

**Block:** 2 de 7
**Depends on:** TASKS-1 (project setup concluído)
**PRD Reference:** `docs/PRD.md` — Seções 3.3, 5.1, 5.2

---

## Objective

Implementar o engine de compilação do QuickNET: templates de código para C# e VB.NET, serviço de compilação via Roslyn, e tipos de resultado. Este bloco **não** inclui execução — apenas compilação em memória.

---

## Domain Model

### Enum: `Language` (`src/QuickNET.Core/Models/Language.cs`)

```csharp
namespace QuickNET.Models;

public enum Language
{
    CSharp,
    VisualBasic
}
```

### Record: `CompilationInput` (`src/QuickNET.Core/Models/CompilationInput.cs`)

```csharp
namespace QuickNET.Models;

public record CompilationInput(
    string SourceCode,
    Language Language
);
```

### Class: `CompilationResult` (`src/QuickNET.Core/Models/CompilationResult.cs`)

```csharp
namespace QuickNET.Models;

public class CompilationResult
{
    public bool Success { get; init; }
    public byte[]? AssemblyBytes { get; init; }
    public List<DiagnosticMessage> Diagnostics { get; init; } = [];
}

public class DiagnosticMessage
{
    public string Severity { get; init; } = "";   // "Error", "Warning", "Info"
    public string Message { get; init; } = "";
    public int? Line { get; init; }
    public int? Column { get; init; }
}
```

---

## Tasks

### 2.1 Criar ITemplateEngine e implementações

Interface em `src/QuickNET.Core/Templates/ITemplateEngine.cs`:

```csharp
namespace QuickNET.Templates;

public interface ITemplateEngine
{
    string GenerateCode(string userCode);
    Language SupportedLanguage { get; }
}
```

O template deve envolver o código do usuário no seguinte formato:

**C# Template** (`src/QuickNET.Core/Templates/CSharpTemplateEngine.cs`):

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class QuickNETSession
{
    public static object Execute()
    {
        // <código do usuário inserido aqui>
    }
}
```

**Regra para inserção:** Se o código do usuário contiver `;` (é um statement), inseri-lo como está dentro de `Execute()`. Se for uma expressão (sem `;`), prefixar com `return` dentro de `Execute()`. Se o código já contiver `return`, mantê-lo.

**VB.NET Template** (`src/QuickNET.Core/Templates/VbTemplateEngine.cs`):

```vb
Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks

Public Module QuickNETSession
    Public Function Execute() As Object
        ' <código do usuário inserido aqui>
    End Function
End Module
```

**Regra para VB.NET:** Se for uma expressão (não contém declarações como `Dim`, `If`, `For`, `Sub`), prefixar com `Return`. Caso contrário, inserir como está.

### 2.2 Criar CompilationService

Classe em `src/QuickNET.Core/Compilation/CompilationService.cs`:

```csharp
namespace QuickNET.Compilation;

public class CompilationService
{
    public CompilationResult Compile(CompilationInput input) { ... }
}
```

Pipeline:
1. Selecionar `ITemplateEngine` baseado em `input.Language`.
2. Gerar código completo com o template.
3. Criar `CSharpCompilation` ou `VisualBasicCompilation` com as opções:
   - `OutputKind.DynamicallyLinkedLibrary`
   - Referências padrão: `System.Runtime`, `System.Console`, `System.IO.FileSystem`, `System.Linq`, `System.Collections`, `System.Text.Encoding` (usar `Assembly.Load` ou `typeof(object).Assembly` para resolver).
4. Compilar para `MemoryStream`.
5. Se houver erros, mapear `Diagnostic[]` para `List<DiagnosticMessage>`, extraindo `Line` e `Column` dos `Location` do Roslyn (subtrair o offset do template para que as posições correspondam ao código original do usuário).
6. Retornar `CompilationResult`.

**Critical: Assembly References**

Para cada target framework assembly necessário, usar:

```csharp
using System.Reflection;

// Assemblies base que TODO snippet pode precisar:
var references = new List<MetadataReference>
{
    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
    MetadataReference.CreateFromFile(typeof(File).Assembly.Location),      // System.IO.FileSystem
    MetadataReference.CreateFromFile(typeof(Encoding).Assembly.Location),   // System.Text.Encoding
    MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),       // System.Threading.Tasks
};
```

Usar `typeof(...).Assembly.Location` garante que a referência venha do runtime atual (.NET 10) sem hardcoding de paths.

### 2.3 Registrar serviços via DI

Em `src/QuickNET.Core`, criar `src/QuickNET.Core/ServiceCollectionExtensions.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using QuickNET.Compilation;
using QuickNET.Templates;

namespace QuickNET;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddQuickNETCore(this IServiceCollection services)
    {
        services.AddSingleton<ITemplateEngine, CSharpTemplateEngine>();
        services.AddSingleton<ITemplateEngine, VbTemplateEngine>();
        services.AddSingleton<CompilationService>();
        return services;
    }
}
```

Adicionar o pacote `Microsoft.Extensions.DependencyInjection` ao `QuickNET.Core` se necessário (já transiente com ASP.NET, mas para classlib pura pode precisar):

```pwsh
dotnet add src/QuickNET.Core package Microsoft.Extensions.DependencyInjection
```

---

## Acceptance Criteria

- [ ] `dotnet build src/QuickNET.Core` compila sem erros nem warnings.
- [ ] `CompilationService.Compile(new CompilationInput("2 + 2", Language.CSharp))` retorna `Success == true` e `AssemblyBytes` não nulo.
- [ ] `CompilationService.Compile(new CompilationInput("2 + 2", Language.VisualBasic))` retorna `Success == true`.
- [ ] Código inválido (ex.: `2 +` ) retorna `Success == false` com pelo menos 1 `DiagnosticMessage` de severidade `"Error"`.
- [ ] `DiagnosticMessage.Line` e `Column` são ajustados para refletir a posição no código original do usuário (sem contar as linhas do template).
- [ ] Método `AddQuickNETCore` registra todos os serviços.

---

## Notes for AI Agent

- A maior complexidade está no offset de linha/coluna dos diagnostics. O template C# tem 9 linhas before user code e o VB.NET tem 8. Guardar o `lineOffset` e subtrair de `diagnostic.Location.GetLineSpan().StartLinePosition.Line`.
- Para o `MemoryStream`, usar `using var ms = new MemoryStream(); compilation.Emit(ms);` — não salvar em disco.
- Para VB.NET, o método `Execute()` deve ser `Function` com `As Object` de retorno. Snippets multi-linha em VB.NET usam `\r\n` como separador de linha (ambiente Windows).
- Se o código do usuário for multi-linha, manter as quebras de linha intactas no template.
- A decisão de "é expressão ou statement" para C#:
  - Se contém `;`, `for`, `if`, `while`, `switch`, `using`, `namespace`, `class` → é statement
  - Caso contrário → é expressão (wrap com `return`)
- A decisão para VB.NET:
  - Se contém `Dim`, `If`, `For`, `While`, `Select`, `Using`, `Namespace`, `Class`, `Module`, `Sub`, `Function`, `End` → é statement
  - Caso contrário → é expressão (wrap com `Return`)
