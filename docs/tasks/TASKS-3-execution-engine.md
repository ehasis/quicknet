# TASKS-3: Execution Engine (AssemblyLoadContext + Reflection)

**Block:** 3 de 7
**Depends on:** TASKS-2 (compilation concluído)
**PRD Reference:** `docs/PRD.md` — Seções 3.3 (items 4 e 5), 3.5, 5.2

---

## Objective

Implementar o executor que carrega assemblies compilados em `AssemblyLoadContext` isolados, invoca o método `Execute()` via reflection, captura o resultado (incluindo `Console.WriteLine`), e libera o contexto para evitar memory leaks.

---

## Domain Model

### Record: `ExecutionInput` (`src/QuickNET.Core/Models/ExecutionInput.cs`)

```csharp
namespace QuickNET.Models;

public record ExecutionInput(byte[] AssemblyBytes);
```

### Record: `ExecutionResult` (`src/QuickNET.Core/Models/ExecutionResult.cs`)

```csharp
namespace QuickNET.Models;

public record ExecutionResult(
    bool Success,
    string? Output,
    string? Error,
    string? ConsoleOutput
);
```

---

## Tasks

### 3.1 Criar QuickNETAssemblyLoadContext

Classe em `src/QuickNET.Core/Execution/QuickNETAssemblyLoadContext.cs`:

```csharp
using System.Reflection;
using System.Runtime.Loader;

namespace QuickNET.Execution;

public class QuickNETAssemblyLoadContext : AssemblyLoadContext
{
    public QuickNETAssemblyLoadContext()
        : base(isCollectible: true)
    {
    }

    public Assembly LoadFromBytes(byte[] assemblyBytes)
    {
        using var ms = new MemoryStream(assemblyBytes);
        return LoadFromStream(ms);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Permite fallback para o contexto default para assemblies do framework
        return null;
    }
}
```

### 3.2 Criar ExecutionService

Classe em `src/QuickNET.Core/Execution/ExecutionService.cs`:

```csharp
namespace QuickNET.Execution;

public class ExecutionService
{
    public ExecutionResult Execute(ExecutionInput input) { ... }
}
```

Pipeline:
1. Criar `QuickNETAssemblyLoadContext` dentro de um `using` (ou `try-finally` com `.Unload()`).
2. Carregar o assembly com `QuickNETAssemblyLoadContext.LoadFromBytes(input.AssemblyBytes)`.
3. Encontrar o tipo `QuickNETSession` no assembly.
4. Encontrar o método `Execute()` (`BindingFlags.Public | BindingFlags.Static`).
5. Capturar `Console.WriteLine` redirecionando `Console.SetOut` para um `StringWriter` antes da execução.
6. Invocar o método via `method.Invoke(null, null)`.
7. Se o retorno for `Task` ou `Task<T>`, fazer `.GetAwaiter().GetResult()` para desbloquear o resultado síncrono.
8. Restaurar `Console.Out` original.
9. Chamar `alc.Unload()` explicitamente.
10. Retornar `ExecutionResult` com:
    - `Success = true`, `Output = result?.ToString() ?? "null"`, `ConsoleOutput` = conteúdo do `StringWriter`
    - `Success = false`, `Error = exception.Message + stack trace` em caso de exceção

**IMPORTANTE:** Após `Unload()`, forçar GC para liberar o assembly:

```csharp
for (int i = 0; i < 3; i++)
{
    GC.Collect();
    GC.WaitForPendingFinalizers();
}
```

### 3.3 Criar o serviço de orquestração: ReplEngine

Classe em `src/QuickNET.Core/ReplEngine.cs`:

```csharp
using QuickNET.Compilation;
using QuickNET.Execution;
using QuickNET.Models;

namespace QuickNET;

public class ReplEngine
{
    private readonly CompilationService _compilation;
    private readonly ExecutionService _execution;

    public ReplEngine(CompilationService compilation, ExecutionService execution)
    {
        _compilation = compilation;
        _execution = execution;
    }

    public ExecutionResult Execute(string sourceCode, Language language)
    {
        var compilationInput = new CompilationInput(sourceCode, language);
        var compilationResult = _compilation.Compile(compilationInput);

        if (!compilationResult.Success)
        {
            var errors = string.Join("\n",
                compilationResult.Diagnostics
                    .Where(d => d.Severity == "Error")
                    .Select(d => $"{d.Severity}: {d.Message} (Line {d.Line}, Col {d.Column})"));
            return new ExecutionResult(false, null, errors, null);
        }

        var executionInput = new ExecutionInput(compilationResult.AssemblyBytes!);
        return _execution.Execute(executionInput);
    }
}
```

### 3.4 Atualizar ServiceCollectionExtensions

Adicionar os novos serviços em `src/QuickNET.Core/ServiceCollectionExtensions.cs`:

```csharp
using QuickNET.Execution;

// Dentro de AddQuickNETCore:
services.AddSingleton<ExecutionService>();
services.AddSingleton<ReplEngine>();
```

---

## Acceptance Criteria

- [ ] `ReplEngine.Execute("2 + 2", Language.CSharp)` retorna `Success == true` com `Output == "4"`.
- [ ] `ReplEngine.Execute("Console.WriteLine(\"hello\");", Language.CSharp)` retorna `Success == true` com `ConsoleOutput` contendo `"hello"`.
- [ ] `ReplEngine.Execute("throw new Exception(\"fail\");", Language.CSharp)` retorna `Success == false` com `Error` contendo `"fail"`.
- [ ] Após N execuções consecutivas (>= 10), o uso de memória não cresce indefinidamente (assembly anterior foi descarregado).
- [ ] `ReplEngine.Execute("2 + 2", Language.VisualBasic)` retorna `Success == true` com `Output == "4"`.
- [ ] Código com `Task`/`async` (ex.: `Task.FromResult(42)`) retorna o Task unwrapped (output deve ser `"42"`, não o nome do tipo Task).

---

## Notes for AI Agent

- O `AssemblyLoadContext` com `isCollectible: true` é essencial. Sem ele, cada execução acumularia assemblies em memória.
- O `Unload()` não é imediato — o GC precisa de múltiplas coletas para realmente liberar. As 3 iterações de `GC.Collect()` + `GC.WaitForPendingFinalizers()` são a prática recomendada.
- Para `Console.SetOut`, guardar a referência original com `var originalOut = Console.Out` e restaurar depois com `Console.SetOut(originalOut)`.
- O tratamento de `Task`/`Task<T>` é necessário porque o usuário pode escrever `await SomethingAsync()` — como o método `Execute()` é síncrono, o Roslyn compilará, mas o retorno será um `Task`. Usar pattern:

```csharp
var result = method.Invoke(null, null);
if (result is Task task)
{
    task.GetAwaiter().GetResult();
    // Se for Task<T>, extrair .Result via reflection
    var resultProperty = task.GetType().GetProperty("Result");
    result = resultProperty?.GetValue(task);
}
```

- O `QuickNETAssemblyLoadContext.Load(AssemblyName)` retorna `null` para delegar ao contexto default — isso é crítico para que assemblies do framework (System.*) sejam resolvidos corretamente.
