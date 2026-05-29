# TASKS-10: Execution Timeout

**Block:** 10 de 12
**Depends on:** TASKS-3 (ExecutionService), TASKS-8 (SessionState, MetaCommandService)
**PRD Reference:** `docs/PRD.md` — Seção 5.7

---

## Objective

Implementar timeout de execução configurável: 30s default, alterável via `/timeout <segundos>` e via ComboBox na UI (TASKS-11). O timeout é aplicado envolvendo a invocação do método `Execute()` em uma `Task` com `Wait(timeout)`. Também completar o handler `/timeout` no `MetaCommandService`.

---

## Tasks

### 10.1 Atualizar ExecutionService para suportar timeout

Arquivo `src/QuickNET.Core/Execution/ExecutionService.cs`:

Adicionar uma sobrecarga ou modificar o método `Execute` para aceitar um parâmetro `timeoutSeconds`:

```csharp
public ExecutionResult Execute(ExecutionInput input, int timeoutSeconds = 0)
{
    QuickNETAssemblyLoadContext? alc = null;
    try
    {
        alc = new QuickNETAssemblyLoadContext();
        var assembly = alc.LoadFromBytes(input.AssemblyBytes);
        var type = assembly.GetType("QuickNETSession");
        var method = type!.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static)!;

        object? result;
        Exception? executionException = null;

        if (timeoutSeconds > 0)
        {
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);
            var task = Task.Run(() => method.Invoke(null, null));

            if (!task.Wait(timeout))
            {
                // Timeout expirado
                return new ExecutionResult(
                    false, null,
                    $"Execution timed out after {timeoutSeconds} seconds.",
                    TryReadConsoleOutput(alc, input.AssemblyBytes)
                );
            }

            if (task.IsFaulted)
                executionException = task.Exception?.InnerException ?? task.Exception;
            else
                result = task.Result;
        }
        else
        {
            // Sem timeout — comportamento original
            try
            {
                result = method.Invoke(null, null);
            }
            catch (TargetInvocationException ex)
            {
                executionException = ex.InnerException ?? ex;
            }
        }

        // ... (resto do pipeline existente: task unwrapping, console output, etc.) ...
    }
    finally
    {
        alc?.Unload();
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
    }
}
```

**Helper `TryReadConsoleOutput`** — tenta ler o campo `__ConsoleOutput` mesmo em caso de timeout. Extrair para método privado reutilizável:

```csharp
private static string? TryReadConsoleOutput(QuickNETAssemblyLoadContext alc, byte[] assemblyBytes)
{
    try
    {
        var assembly = alc.LoadFromBytes(assemblyBytes);
        var type = assembly.GetType("QuickNETSession");
        var field = type?.GetField("__ConsoleOutput", BindingFlags.Public | BindingFlags.Static);
        return field?.GetValue(null) as string;
    }
    catch
    {
        return null;
    }
}
```

**Nota:** Este método também deve ser usado no caminho normal de execução para evitar duplicação de código de leitura do `__ConsoleOutput`. Refatorar se apropriado.

### 10.2 Atualizar ReplEngine para passar timeout

Arquivo `src/QuickNET.Core/ReplEngine.cs`:

Adicionar `SessionState` como dependência e passar `TimeoutSeconds` para o `ExecutionService`:

```csharp
public class ReplEngine
{
    private readonly CompilationService _compilation;
    private readonly ExecutionService _execution;
    private readonly SessionState _sessionState;

    public ReplEngine(CompilationService compilation, ExecutionService execution, SessionState sessionState)
    {
        _compilation = compilation;
        _execution = execution;
        _sessionState = sessionState;
    }

    public ExecutionResult Execute(string sourceCode, Language language)
    {
        var compilationInput = new CompilationInput(
            sourceCode,
            language,
            _sessionState.ExtraReferences,
            _sessionState.ExtraImports
        );

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
        return _execution.Execute(executionInput, _sessionState.TimeoutSeconds);
    }
}
```

### 10.3 Completar handler /timeout no MetaCommandService

Substituir o stub `NotYetImplemented("timeout")` pela implementação real em `src/QuickNET.Core/MetaCommands/MetaCommandService.cs`:

```csharp
private MetaCommandResult ExecuteTimeout(string? args)
{
    if (string.IsNullOrWhiteSpace(args))
    {
        var currentSecs = _sessionState.TimeoutSeconds;
        var currentLabel = currentSecs == 0 ? "no limit" : $"{currentSecs}s";
        return new MetaCommandResult
        {
            Command = "timeout",
            DisplayText = $"Current timeout: {currentLabel}. Usage: /timeout <seconds> (0 = no limit)",
            Success = true
        };
    }

    var trimmed = args.Trim();
    if (!int.TryParse(trimmed, out var seconds) || seconds < 0)
    {
        return new MetaCommandResult
        {
            Command = "timeout",
            DisplayText = $"Invalid timeout value '{trimmed}'. Expected a non-negative number (0 = no limit).",
            Success = false
        };
    }

    _sessionState.TimeoutSeconds = seconds;
    var label = seconds == 0 ? "no limit" : $"{seconds}s";
    return new MetaCommandResult
    {
        Command = "timeout",
        DisplayText = $"Execution timeout set to {label}.",
        Success = true
    };
}
```

### 10.4 Atualizar ServiceCollectionExtensions se necessário

Verificar se `SessionState` já está registrado como singleton (deve estar do TASKS-8). Nenhuma alteração adicional necessária neste bloco — o `ReplEngine` e `ExecutionService` já estão registrados.

---

## Acceptance Criteria

- [ ] `ReplEngine.Execute("2 + 2", Language.CSharp)` com timeout de 30s retorna `Success == true, Output == "4"` (comportamento normal).
- [ ] `ReplEngine.Execute("System.Threading.Thread.Sleep(2000); return 42;", Language.CSharp)` com timeout de 1s retorna `Success == false` com mensagem contendo `"timed out after 1 seconds"`.
- [ ] `ReplEngine.Execute("2 + 2", Language.CSharp)` com `timeoutSeconds = 0` (sem limite) funciona normalmente.
- [ ] `/timeout` sem argumentos exibe o timeout atual.
- [ ] `/timeout 60` define o timeout para 60 segundos e exibe confirmação.
- [ ] `/timeout 0` define timeout como "sem limite".
- [ ] `/timeout abc` exibe erro de valor inválido.
- [ ] `/timeout -5` exibe erro de valor inválido (negativo).
- [ ] O timeout persiste ao fechar e reabrir a aplicação (via `SessionState`).
- [ ] Após timeout, o `AssemblyLoadContext` anterior é descarregado (3 rounds de GC executados).

---

## Notes for AI Agent

- **Limitação conhecida:** Loops infinitos puros (`while(true){}`) não são interrompidos pelo timeout. A `Task` fica em execução mesmo após `Wait(timeout)` expirar. O `AssemblyLoadContext` é unloaded e a task órfã eventualmente será coletada pelo GC. Esta limitação está documentada no PRD.
- **`Thread.Sleep` vs loop infinito:** `Thread.Sleep` funciona com timeout porque o thread fica bloqueado e o `Task.Wait` detecta o timeout. Já `while(true){}` mantém o thread ocupado e o `Wait` nunca retorna até que o loop termine (nunca). Para testing, usar `Thread.Sleep` como cenário de timeout.
- O `TryReadConsoleOutput` é chamado mesmo no caminho de timeout porque o código do usuário pode ter escrito algo no console antes de travar. Isso garante que output parcial não seja perdido.
- O tratamento de `Task`/`Task<T>` (unwrapping) existente no `ExecutionService` deve continuar funcionando. O novo código de timeout deve ser inserido **antes** do unwrapping — se o `Task.Run` retornar um `Task<T>`, o unwrapping é feito no resultado.
- O `CancellationToken` **não** é injetado no código do usuário. O cancelamento é puramente externo (timeout do `Task.Wait`).
- O parâmetro `timeoutSeconds` no método `Execute` tem valor default `0` para manter compatibilidade com chamadas existentes que não especificam timeout.
