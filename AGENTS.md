# AGENTS.md

## Project

QuickNET — a .NET desktop REPL for C# and VB.NET using Roslyn in-memory compilation and Avalonia UI. Windows-only MVP targeting .NET 10 with MSTest.

## Build & Run

```bash
dotnet build                              # solution-wide build
dotnet test                               # run all tests
dotnet run --project src/QuickNET.App      # launch the desktop app
```

- SDK pinned to `10.0.300` via `global.json`. Use the same or newer 10.0 band.
- Solution uses `.slnx` format (`quicknet.slnx`), not `.sln`. Standard `dotnet` CLI works normally.
- `UseArtifactsOutput=true` in `Directory.Build.props` — build outputs go under `artifacts/`, not per-project `bin/obj`.

## Architecture

```
QuickNET.Core (classlib, net10.0)
  ├── Models/          -- records/enums: Language, CompilationInput/Result, ExecutionInput/Result
  ├── Templates/       -- ITemplateEngine → CSharpTemplateEngine, VbTemplateEngine
  ├── Compilation/     -- CompilationService (Roslyn CSharpCompilation / VisualBasicCompilation)
  ├── Execution/       -- ExecutionService, QuickNETAssemblyLoadContext (isolated + collectible)
  └── ReplEngine.cs    -- public orchestrator: Compile → Execute pipeline
QuickNET.App (WinExe, net10.0-windows)
  ├── Models/          -- ConversationItem (display model with Foreground brush)
  ├── Views/           -- MainWindow (full layout with compiled bindings, key handling, auto-scroll)
  ├── ViewModels/      -- MainWindowViewModel (ObservableObject, [RelayCommand], [ObservableProperty])
  └── Program.cs       -- entry point with DI wiring via ServiceCollection
QuickNET.Tests (MSTest.Sdk, net10.0-windows)
  └── references both Core and App projects
```

- `QuickNET.App` depends on `QuickNET.Core`. Tests depend on both.
- DI registration: `ServiceCollectionExtensions.AddQuickNETCore()` registers all services as singletons. The app layer hasn't wired DI yet (Program.cs is still minimal).

## Testing

- **MSTest.Sdk** (v4.2.3), not `Microsoft.NET.Test.Sdk`. The project file uses `<Project Sdk="MSTest.Sdk">`.
- Sealed test classes (`[TestClass] sealed`), no `[TestInitialize]` — use constructor DI.
- Naming: `MethodName_Scenario_ExpectedBehavior`.
- Method-level parallelization enabled: `[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]`.
- MSTest skill loaded at `.agents/skills/mstest/SKILL.md` — follow those conventions.
- Every new implementation in `QuickNET.Core` must have corresponding tests with at least 80% coverage.

## Important Gotchas

### Console capture via template, not TextWriter decorator
Console output is captured inside the template's generated `Execute()` method via a `__ConsoleOutput` static field. Do not try to redirect `Console.SetOut` from the host process — `AssemblyLoadContext` isolates type identity, so `System.Console` in the loaded assembly is not the same as the host's.

### ALC cleanup
`QuickNETAssemblyLoadContext` uses `isCollectible: true` with 3 rounds of forced GC after each execution. This is intentional to avoid memory leaks from accumulated assemblies.

### Avalonia version mismatch
Avalonia packages are v12.0.3 except `Avalonia.Diagnostics` which is v11.3.17 — presumably deliberate. Do not "fix" without checking.

### Each execution is isolated
No shared state between inputs. Each snippet is compiled and executed independently (per PRD non-goal). The template wraps user code in a standalone `static class QuickNETSession`.

### File-scoped namespaces
All C# files use file-scoped namespaces (`namespace QuickNET.Models;`). Match this style.

## Task Tracking

Work is organized in sequential task blocks under `docs/tasks/`. Current status:
- TASKS-1 (setup), TASKS-2 (compilation), TASKS-3 (execution) — **done**
- TASKS-4 (history persistence) — **done**
- TASKS-5 (UI shell) — **done**
- TASKS-6 (UI wiring / ViewModels) — **done**
- TASKS-7 (tests) — **done** (52 tests, 0 failures)

## MVP Non-Goals (do not implement)
No IntelliSense, no syntax highlighting, no CLI mode, no meta-commands (`/clear`, `/help`), no cross-platform (Windows only), no shared context between executions, no NuGet import at runtime.
