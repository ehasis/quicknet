# TASKS-9: Dynamic Assembly References & Namespace Imports

**Block:** 9 de 12
**Depends on:** TASKS-2 (CompilationService, ITemplateEngine), TASKS-8 (SessionState, MetaCommandService)
**PRD Reference:** `docs/PRD.md` — Seções 5.6, 5.4

---

## Objective

Implementar o serviço de resolução de assemblies (`AssemblyResolutionService`), estender o `CompilationService` e os templates para aceitar referências e imports dinâmicos, e completar os meta-comandos `/reference`, `/import` (`/using`), `/references` e `/imports` no `MetaCommandService`.

---

## Tasks

### 9.1 Estender CompilationInput

Atualizar `src/QuickNET.Core/Models/CompilationInput.cs` para incluir referências e imports extras:

```csharp
namespace QuickNET.Models;

public record CompilationInput(
    string SourceCode,
    Language Language,
    IReadOnlyList<string>? ExtraReferences = null,
    IReadOnlyList<string>? ExtraImports = null
);
```

### 9.2 Estender ITemplateEngine para suportar imports extras

Atualizar `src/QuickNET.Core/Templates/ITemplateEngine.cs`:

```csharp
namespace QuickNET.Templates;

public interface ITemplateEngine
{
    string GenerateCode(string userCode, IReadOnlyList<string>? extraImports = null);
    Language SupportedLanguage { get; }
}
```

### 9.3 Atualizar CSharpTemplateEngine para injetar imports extras

Arquivo `src/QuickNET.Core/Templates/CSharpTemplateEngine.cs`:

O template C# atualmente gera usings fixos no topo:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
```

**Alteração:** Após os usings padrão, adicionar dinamicamente os imports extras fornecidos via parâmetro `extraImports`:

```csharp
public string GenerateCode(string userCode, IReadOnlyList<string>? extraImports = null)
{
    var defaultUsings = @"using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
";

    var extraUsingLines = "";
    if (extraImports != null && extraImports.Count > 0)
    {
        extraUsingLines = string.Join("\n",
            extraImports.Distinct().Select(ns => $"using {ns};")) + "\n";
    }

    // Restante do template: class QuickNETSession, etc.
    // ...
}
```

**Importante:** Usar `Distinct()` nos imports para evitar duplicatas. Imports extras vêm depois dos padrão.

### 9.4 Atualizar VbTemplateEngine para injetar imports extras

Arquivo `src/QuickNET.Core/Templates/VbTemplateEngine.cs`:

Mesmo princípio — após os `Imports` padrão, adicionar imports extras:

```csharp
public string GenerateCode(string userCode, IReadOnlyList<string>? extraImports = null)
{
    var defaultImports = @"Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Text
Imports System.Threading.Tasks
";

    var extraImportLines = "";
    if (extraImports != null && extraImports.Count > 0)
    {
        extraImportLines = string.Join("\n",
            extraImports.Distinct().Select(ns => $"Imports {ns}")) + "\n";
    }

    // Restante do template...
}
```

### 9.5 Criar AssemblyResolutionService

Arquivo `src/QuickNET.Core/Compilation/AssemblyResolutionService.cs`:

```csharp
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace QuickNET.Compilation;

public class AssemblyResolutionService
{
    private readonly HashSet<string> _loadedAssemblyNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<MetadataReference> _extraReferences = [];

    public IReadOnlyList<MetadataReference> ExtraReferences => _extraReferences.AsReadOnly();
    public IReadOnlyCollection<string> LoadedAssemblyNames => _loadedAssemblyNames;

    public MetadataReference? Resolve(string assemblyName)
    {
        // Tenta carregar via Assembly.Load do runtime
        try
        {
            var assembly = Assembly.Load(assemblyName);
            if (string.IsNullOrEmpty(assembly.Location))
                return null;

            _loadedAssemblyNames.Add(assemblyName);
            var reference = MetadataReference.CreateFromFile(assembly.Location);
            _extraReferences.Add(reference);
            return reference;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (FileLoadException)
        {
            return null;
        }
    }

    public List<MetadataReference> GetAllReferences()
    {
        return [.. _extraReferences];
    }
}
```

**Nota:** O `AssemblyResolutionService` mantém estado interno das referências resolvidas. As referências padrão (System.Runtime, System.Console, etc.) permanecem fixas no `CompilationService` e **não** são duplicadas aqui.

**Assemblies notáveis que devem ser suportados:**
- `System.Text.Json`
- `System.Net.Http`
- `System.Text.RegularExpressions`
- `Microsoft.CSharp`
- Qualquer assembly acessível via `Assembly.Load()` no runtime .NET 10.

### 9.6 Atualizar CompilationService para usar referências e imports extras

Arquivo `src/QuickNET.Core/Compilation/CompilationService.cs`:

**Injetar `AssemblyResolutionService`** no construtor.

Atualizar o método `Compile`:

```csharp
public CompilationResult Compile(CompilationInput input)
{
    // 1. Selecionar template engine
    var engine = _templateEngines.First(e => e.SupportedLanguage == input.Language);

    // 2. Gerar código com imports extras
    var fullCode = engine.GenerateCode(input.SourceCode, input.ExtraImports);

    // 3. Coletar referências: padrão + extras
    var references = new List<MetadataReference>(GetDefaultReferences());
    if (input.ExtraReferences != null && input.ExtraReferences.Count > 0)
    {
        foreach (var refName in input.ExtraReferences)
        {
            var resolved = _assemblyResolver.Resolve(refName);
            if (resolved != null)
                references.Add(resolved);
            // Se não resolveu, continuar — o erro de compilação será capturado depois
        }
    }

    // 4. Criar compilação
    // ... (resto igual, mas usando a lista references ampliada)

    // 5. Ajustar offset de linha (considerar imports extras no offset)
    // ATENÇÃO: imports extras aumentam o número de linhas antes do código do usuário.
    // Cada import extra adiciona 1 linha ao template.
    // lineOffset deve ser recalculado:
    //   C#: 18 + (extraImports?.Count ?? 0)
    //   VB: 15 + (extraImports?.Count ?? 0)
}
```

**Cálculo do lineOffset ajustado:**

```csharp
int baseLineOffset = input.Language == Language.CSharp ? 18 : 15;
int extraImportLines = input.ExtraImports?.Count ?? 0;
int lineOffset = baseLineOffset + extraImportLines;
```

### 9.7 Atualizar ReplEngine para passar SessionState

Arquivo `src/QuickNET.Core/ReplEngine.cs`:

Injetar `SessionState` e passar `ExtraReferences` e `ExtraImports` para o `CompilationInput`:

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
            SourceCode: sourceCode,
            Language: language,
            ExtraReferences: _sessionState.ExtraReferences,
            ExtraImports: _sessionState.ExtraImports
        );

        var compilationResult = _compilation.Compile(compilationInput);

        if (!compilationResult.Success)
        {
            // ... existing error handling ...
        }

        var executionInput = new ExecutionInput(compilationResult.AssemblyBytes!);
        return _execution.Execute(executionInput);
    }
}
```

### 9.8 Completar os handlers no MetaCommandService

Atualizar `src/QuickNET.Core/MetaCommands/MetaCommandService.cs`:

Injetar `AssemblyResolutionService` e substituir os stubs `NotYetImplemented` por implementações reais:

```csharp
public class MetaCommandService
{
    private readonly SessionState _sessionState;
    private readonly AssemblyResolutionService _assemblyResolver;

    public MetaCommandService(SessionState sessionState, AssemblyResolutionService assemblyResolver)
    {
        _sessionState = sessionState;
        _assemblyResolver = assemblyResolver;
    }

    public MetaCommandResult Execute(string input)
    {
        var (command, args) = MetaCommandParser.Parse(input);

        if (string.IsNullOrEmpty(command))
            return new MetaCommandResult { ... };

        return command switch
        {
            "clear" => ExecuteClear(),
            "help" => ExecuteHelp(),
            "lang" => ExecuteLang(args),
            "reference" => ExecuteReference(args),
            "import" or "using" => ExecuteImport(args),
            "references" => ExecuteReferences(),
            "imports" => ExecuteImports(),
            "timeout" => NotYetImplemented(command), // TASKS-10
            _ => new MetaCommandResult { ... }
        };
    }

    private MetaCommandResult ExecuteReference(string? args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new MetaCommandResult
            {
                Command = "reference",
                DisplayText = "Usage: /reference <assembly_name>\nExample: /reference System.Text.Json",
                Success = false
            };

        var assemblyName = args.Trim();
        var resolved = _assemblyResolver.Resolve(assemblyName);

        if (resolved == null)
            return new MetaCommandResult
            {
                Command = "reference",
                DisplayText = $"Assembly '{assemblyName}' not found in the runtime.",
                Success = false
            };

        _sessionState.AddReference(assemblyName);
        return new MetaCommandResult
        {
            Command = "reference",
            DisplayText = $"Added reference to '{assemblyName}'.",
            Success = true
        };
    }

    private MetaCommandResult ExecuteImport(string? args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new MetaCommandResult
            {
                Command = "import",
                DisplayText = "Usage: /import <namespace> (alias: /using)\nExample: /import System.Text.Json",
                Success = false
            };

        var ns = args.Trim();
        _sessionState.AddImport(ns);
        return new MetaCommandResult
        {
            Command = "import",
            DisplayText = $"Added import for '{ns}'.",
            Success = true
        };
    }

    private MetaCommandResult ExecuteReferences()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Default references:");
        sb.AppendLine("  System.Runtime, System.Console, System.Linq, System.IO.FileSystem,");
        sb.AppendLine("  System.Text.Encoding, System.Threading.Tasks");
        sb.AppendLine();

        var extraRefs = _sessionState.ExtraReferences;
        if (extraRefs.Count > 0)
        {
            sb.AppendLine("Extra references (via /reference):");
            foreach (var r in extraRefs)
                sb.AppendLine($"  {r}");
        }
        else
        {
            sb.AppendLine("No extra references added.");
        }

        return new MetaCommandResult
        {
            Command = "references",
            DisplayText = sb.ToString().TrimEnd(),
            Success = true
        };
    }

    private MetaCommandResult ExecuteImports()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Default imports:");
        sb.AppendLine("  System, System.Collections.Generic, System.IO, System.Linq,");
        sb.AppendLine("  System.Text, System.Threading.Tasks");
        sb.AppendLine();

        var extraImports = _sessionState.ExtraImports;
        if (extraImports.Count > 0)
        {
            sb.AppendLine("Extra imports (via /import):");
            foreach (var imp in extraImports)
                sb.AppendLine($"  {imp}");
        }
        else
        {
            sb.AppendLine("No extra imports added.");
        }

        return new MetaCommandResult
        {
            Command = "imports",
            DisplayText = sb.ToString().TrimEnd(),
            Success = true
        };
    }
}
```

### 9.9 Atualizar ServiceCollectionExtensions

Registrar `AssemblyResolutionService`:

```csharp
// Em AddQuickNETCore:
services.AddSingleton<AssemblyResolutionService>();
```

**Nota:** `AssemblyResolutionService` é singleton para que as referências resolvidas persistam durante toda a sessão (evita resolver o mesmo assembly múltiplas vezes).

---

## Acceptance Criteria

- [ ] `/reference System.Text.Json` adiciona o assembly e permite compilar código que usa `JsonSerializer`.
- [ ] `/reference Foo.Bar` (assembly inexistente) exibe `Assembly 'Foo.Bar' not found in the runtime.`
- [ ] `/reference` sem argumentos exibe mensagem de uso.
- [ ] `/import System.Text.Json` adiciona o namespace e permite usar `JsonSerializer` sem qualificação completa no código.
- [ ] `/using System.Text.Json` (alias) funciona identicamente a `/import`.
- [ ] `/import` sem argumentos exibe mensagem de uso.
- [ ] `/references` lista os assemblies padrão e os extras adicionados.
- [ ] `/imports` lista os namespaces padrão e os extras adicionados.
- [ ] Referências e imports adicionados persistem ao fechar e reabrir a aplicação.
- [ ] Adicionar a mesma referência ou import duas vezes não duplica — segunda chamada é ignorada.
- [ ] Compilar código que usa APIs de um assembly referenciado dinamicamente funciona (ex.: `/reference System.Text.Json` seguido de `JsonSerializer.Serialize(new { a = 1 })`).
- [ ] O offset de linha nos diagnósticos de compilação permanece correto mesmo com imports extras adicionados.

---

## Notes for AI Agent

- O `AssemblyResolutionService` é um cache de resolução. Uma vez que um assembly é resolvido com sucesso, ele fica disponível para todas as compilações subsequentes.
- `Assembly.Load(assemblyName)` funciona para assemblies do framework .NET que estão no GAC ou no runtime directory. Assemblies de terceiros ou locais precisariam de `Assembly.LoadFrom(path)`, que está fora do escopo da v1.1.
- As referências padrão continuam sendo definidas no `CompilationService.GetDefaultReferences()` (ou inline no método `Compile`). As extras são adicionadas cumulativamente.
- O `AssemblyResolutionService.Resolve()` retorna `null` para assemblies não encontrados. O `CompilationService` deve ignorar referências extras que não foram resolvidas (o erro aparecerá como erro de compilação "type not found" mais tarde, o que é aceitável).
- **Cuidado com o lineOffset:** Cada import extra adiciona exatamente 1 linha ao template (uma linha de `using` ou `Imports`). O offset deve ser ajustado dinamicamente: `baseOffset + extraImports.Count`.
- A ordem dos usings/imports extras deve ser a mesma ordem em que foram adicionados pelo usuário (preservar a ordem da lista em `SessionState.ExtraImports`).
- O VB.NET não tem alias `/using` — apenas `/import` é reconhecido, mas internamente o parser aceita ambos os tokens (`"import" or "using"`).
- Para o teste de integração "compilar com assembly dinâmico", usar `System.Text.Json` que está disponível no runtime .NET 10.
