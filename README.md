# QuickNET

A .NET desktop REPL for **C# and VB.NET** using Roslyn in-memory compilation and Avalonia UI.

Write and execute code snippets interactively — no project setup, no boilerplate. Windows-only MVP targeting .NET 10.

## Features

- **Dual-language REPL** — C# and VB.NET snippets compiled and executed in-memory via Roslyn
- **Autocomplete / IntelliSense** — popup triggered on `.` and other characters, powered by Roslyn's `AdhocWorkspace` + `CompletionService`
- **Signature help tooltip** — method and constructor overload signatures displayed below the input field, triggered by `(` and `,`
- **Input history navigation** — Up/Down arrow keys recall previously executed snippets
- **Isolated execution** — each snippet runs in its own collectible `AssemblyLoadContext` with forced GC cleanup to prevent memory leaks
- **Execution timeout** — default 30s, configurable via `#timeout` to prevent runaway snippets
- **Meta-commands** — `#clear`, `#reset`, `#timeout`, `#language`, `#theme`, `#ref`, `#import`, and more
- **Theme system** — Light, Dark, and System themes with live switching
- **History persistence** — JSON-based execution history with timestamp, language, input, output, and error status
- **Dynamic references & imports** — add/remove assembly references and namespace imports at runtime

## Build & Run

```bash
dotnet build                               # solution-wide build
dotnet test                                # run all tests
dotnet run --project src/QuickNET.App       # launch the desktop app
```

Requirements:
- .NET SDK `10.0.300` or newer 10.0 band
- Windows (the app targets `net10.0-windows`)

## Architecture

```
QuickNET.Core (classlib, net10.0)
  ├── Models/         -- records/enums: Language, CompilationInput/Result, ExecutionInput/Result, etc.
  ├── Templates/      -- code templates wrapping user snippets into standalone QuickNETSession class
  ├── Compilation/    -- Roslyn in-memory compilation with assembly reference resolution
  ├── Completion/     -- Roslyn autocomplete and signature help (AdhocWorkspace + CompletionService)
  ├── Execution/      -- isolated, collectible AssemblyLoadContext execution with Console output capture
  ├── History/        -- JSON-persisted execution history and input history navigation
  ├── MetaCommands/   -- REPL meta-command parser and service (#clear, #reset, #ref, #import, etc.)
  ├── Session/        -- session state (references, imports, timeout, theme, language)
  ├── Theme/          -- Light/Dark/System theme management
  └── ReplEngine.cs   -- orchestrator: Compile → Execute pipeline

QuickNET.App (WinExe, net10.0-windows)
  ├── Views/          -- MainWindow (Avalonia UI with Popup, key handling, auto-scroll)
  ├── ViewModels/     -- MainWindowViewModel, CompletionViewModel, SignatureHelpViewModel
  ├── Controls/       -- CompletionPopup (autocomplete flyout)
  └── Program.cs      -- entry point with DI wiring via ServiceCollection

QuickNET.Tests (MSTest.Sdk, net10.0-windows)
  └── Unit & integration tests (52 tests, 0 failures)
```

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 |
| UI | Avalonia UI 12 (Fluent theme) |
| Compilation | Microsoft.CodeAnalysis (Roslyn) |
| MVVM | CommunityToolkit.Mvvm |
| DI | Microsoft.Extensions.DependencyInjection |
| Testing | MSTest.Sdk 4.x |

## Meta-Commands

| Command | Description |
|---|---|
| `#clear` | Clear the conversation |
| `#reset` | Reset session state |
| `#timeout <seconds>` | Set execution timeout |
| `#language csharp\|vb` | Switch language |
| `#theme light\|dark\|system` | Switch theme |
| `#ref <assembly>` | Add assembly reference |
| `#ref remove <assembly>` | Remove assembly reference |
| `#import <namespace>` | Add namespace import |
| `#import remove <namespace>` | Remove namespace import |
| `#help` | Show available commands |

## Development

> This project was developed using [OpenCode](https://opencode.ai) + DeepSeek V4 Pro Max.

All C# files use file-scoped namespaces. Build outputs go under `artifacts/` (configured via `Directory.Build.props`). Tests use MSTest.Sdk with method-level parallelization and constructor-based DI.

## License

MIT — see [LICENSE](LICENSE) for details.
