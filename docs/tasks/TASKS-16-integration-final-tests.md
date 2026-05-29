# TASKS-16: v1.2 Integration & Final Tests

**Block:** 16 de 16
**Depends on:** TASKS-13 (Theme System), TASKS-14 (Autocomplete), TASKS-15 (Input History)
**PRD Reference:** `docs/PRD.md` — Seções 5.8, 5.9, 5.10, 3.1 (Architecture), 6 (Testing)

---

## Objective

Integrar todos os novos serviços da v1.2 (ThemeService, CompletionEngine, InputHistoryService) na camada de aplicação: DI registration, atualização do MainWindowViewModel e MainWindow, testes de integração end-to-end, e verificação de que todos os testes existentes continuam passando.

---

## Tasks

### 16.1 Atualizar `SessionSettings` com `Theme`

Arquivo `src/QuickNET.Core/Models/SessionSettings.cs`:

Adicionar a propriedade (se ainda não foi adicionada em TASKS-13):

```csharp
public string Theme { get; set; } = "System";  // "System", "Light", "Dark"
```

### 16.2 Registrar novos serviços no DI

Arquivo `src/QuickNET.Core/ServiceCollectionExtensions.cs`:

Adicionar registrations:

```csharp
services.AddSingleton<ThemeService>();
services.AddSingleton<CompletionEngine>();
services.AddSingleton<InputHistoryService>();
```

O arquivo final deve conter todas as registrations:

```csharp
public static IServiceCollection AddQuickNETCore(this IServiceCollection services)
{
    services.AddSingleton<ITemplateEngine, CSharpTemplateEngine>();
    services.AddSingleton<ITemplateEngine, VbTemplateEngine>();
    services.AddSingleton<CompilationService>();
    services.AddSingleton<ExecutionService>();
    services.AddSingleton<ReplEngine>();
    services.AddSingleton<HistoryManager>();
    services.AddSingleton<HistoryService>();
    services.AddSingleton<SessionState>();
    services.AddSingleton<MetaCommandService>();
    services.AddSingleton<AssemblyResolutionService>();
    // v1.2:
    services.AddSingleton<ThemeService>();
    services.AddSingleton<CompletionEngine>();
    services.AddSingleton<InputHistoryService>();
    return services;
}
```

### 16.3 Atualizar `MainWindowViewModel` — construtor e dependências

Arquivo `src/QuickNET.App/ViewModels/MainWindowViewModel.cs`:

Adicionar novos parâmetros no construtor:

```csharp
public MainWindowViewModel(
    ReplEngine engine,
    HistoryService history,
    MetaCommandService metaCommandService,
    SessionState sessionState,
    ThemeService themeService,          // novo
    CompletionEngine completionEngine,   // novo
    InputHistoryService inputHistory)    // novo
{
    _engine = engine;
    _history = history;
    _metaCommandService = metaCommandService;
    _sessionState = sessionState;
    _themeService = themeService;
    _completionEngine = completionEngine;
    _inputHistory = inputHistory;

    LoadHistory();
}
```

### 16.4 Atualizar `MetaCommandService` — dependência do ThemeService

Arquivo `src/QuickNET.Core/MetaCommands/MetaCommandService.cs`:

Adicionar `ThemeService` como dependência no construtor:

```csharp
private readonly ThemeService _themeService;

public MetaCommandService(
    SessionState sessionState,
    AssemblyResolutionService assemblyResolver,
    ThemeService themeService)
{
    _sessionState = sessionState;
    _assemblyResolver = assemblyResolver;
    _themeService = themeService;
}
```

### 16.5 Atualizar `App.axaml.cs` — inicialização do tema

Arquivo `src/QuickNET.App/App.axaml.cs`:

```csharp
public class App : Application
{
    private readonly IServiceProvider? _serviceProvider;

    public App(IServiceProvider? serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = _serviceProvider!.GetRequiredService<MainWindowViewModel>();
            var themeService = _serviceProvider.GetRequiredService<ThemeService>();

            // Aplicar tema inicial
            ApplyTheme(themeService.CurrentTheme);

            // Reagir a mudanças de tema
            themeService.ThemeChanged += (_, theme) => ApplyTheme(theme);

            desktop.MainWindow = new MainWindow
            {
                DataContext = vm
            };

            // Assinar evento de fechamento
            vm.CloseRequested += (_, _) => desktop.MainWindow.Close();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void ApplyTheme(AppTheme theme)
    {
        if (Application.Current is not { } app) return;

        var fluentTheme = app.Styles.OfType<FluentTheme>().FirstOrDefault();
        if (fluentTheme is null)
        {
            fluentTheme = new FluentTheme();
            app.Styles.Insert(0, fluentTheme);
        }

        fluentTheme.Mode = theme switch
        {
            AppTheme.Dark => FluentThemeMode.Dark,
            AppTheme.Light => FluentThemeMode.Light,
            AppTheme.System => FluentThemeMode.System
        };
    }
}
```

### 16.6 Atualizar `App.axaml` — garantir FluentTheme

Arquivo `src/QuickNET.App/App.axaml`:

Verificar se `<FluentTheme />` está declarado no `Application.Styles`. Se não estiver, o `App.axaml.cs` cria um programaticamente. Se estiver, o `App.axaml.cs` encontra e modifica. Preferível manter no XAML para ordem correta de styles.

```xml
<Application.Styles>
    <FluentTheme />
</Application.Styles>
```

### 16.7 Atualizar `MainWindow.axaml` — adicionar Popup de completion

Arquivo `src/QuickNET.App/Views/MainWindow.axaml`:

Adicionar o Popup overlay (conforme TASKS-14.7) e o namespace `controls`:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:controls="clr-namespace:QuickNET.Controls"
        ...>
```

Adicionar o Popup ANTES do fechamento da Grid principal:

```xml
    <!-- Completion Popup overlay -->
    <Popup x:Name="CompletionOverlay"
           IsOpen="{Binding Completion.IsVisible}"
           StaysOpen="False"
           Grid.RowSpan="3">
        <controls:CompletionPopup x:Name="CompletionPopupControl"
                                   DataContext="{Binding Completion}" />
    </Popup>
</Grid>
```

### 16.8 Atualizar `Program.cs` (se necessário)

Arquivo `src/QuickNET.App/Program.cs`:

Verificar que as novas dependências são resolvidas automaticamente. O `BuildAvaloniaApp` deve receber o `IServiceProvider`. Se precisar de ajustes:

```csharp
var services = new ServiceCollection();
services.AddQuickNETCore();
services.AddSingleton<MainWindowViewModel>();
var provider = services.BuildServiceProvider();

// Passar o provider para App e para Avalonia
BuildAvaloniaApp(provider).StartWithClassicDesktopLifetime(args);
```

### 16.9 Adicionar `InternalsVisibleTo` para novos serviços (se necessário)

Verificar `QuickNET.Core.csproj`:

```xml
<ItemGroup>
    <InternalsVisibleTo Include="QuickNET.Tests" />
</ItemGroup>
```

O `InputHistoryService` tem construtor `internal` para testes — verificar que o `InternalsVisibleTo` já está configurado (foi em TASKS-12).

### 16.10 Testes de integração v1.2

#### 16.10.1 `ThemeIntegrationTests`

Arquivo `tests/QuickNET.Tests/Integration/ThemeIntegrationTests.cs` (criar diretório `Integration/`):

Classe: `[TestClass] sealed`. Construtor cria `SessionState` (path temporário) + `ThemeService` + `MetaCommandService`.

| Método | Descrição |
|---|---|
| `FullThemeLifecycle_DefaultToDarkAndBack` | 1. Criar SessionState (default System) → ThemeService.CurrentTheme == System. 2. `/theme dark` → SessionState.CurrentTheme == "Dark". 3. Reiniciar (novo SessionState com mesmo path) → carrega "Dark". 4. `/theme light` → "Light". 5. Reiniciar → carrega "Light". |
| `ThemePersisted_AfterRestart` | `/theme dark` → salvar → novo SessionState com mesmo path → CurrentTheme == Dark |

#### 16.10.2 `CompletionIntegrationTests`

Arquivo `tests/QuickNET.Tests/Integration/CompletionIntegrationTests.cs`:

Classe: `[TestClass] sealed`. Depende de `CompletionEngine` real.

| Método | Descrição |
|---|---|
| `Completion_Keyword_CSharp` | `"usi"` com cursor na posição 3, linguagem C# → resultados contêm `"using"` |
| `Completion_MemberAccess_Console` | `"Console."` com cursor após `.`, linguagem C# → resultados contêm `"WriteLine"` |
| `Completion_RespectsLanguage_VB` | `"Console."` com cursor após `.`, linguagem VB → resultados incluem membros de `Console` |
| `Completion_Keyword_VB` | `"Dim"` com cursor na posição 3, linguagem VB → resultados contêm keyword VB |
| `Completion_WithExtraImport` | Adicionar `System.Text.Json` como extra import, código `"JsonSerializer."` → resultados contêm `"Serialize"` |
| `Completion_Cancelled_ReturnsEmpty` | Passar token cancelado → lança `OperationCanceledException` ou retorna vazio |

#### 16.10.3 `InputHistoryIntegrationTests`

Arquivo `tests/QuickNET.Tests/Integration/InputHistoryIntegrationTests.cs`:

| Método | Descrição |
|---|---|
| `History_PersistsAcrossSessions` | `Record("2+2")` → novo `InputHistoryService` com mesmo path → `GetEntries()` contém `"2+2"` |
| `History_NavigateAndExecute` | `Record("a")`, `Record("b")`. `NavigateOlder("")` retorna `"b"`. `Record("c")` reseta navegação. |
| `History_DraftRestored_AfterNavigate` | `NavigateOlder("draft")`, 2x `NavigateNewer()` → retorna `"draft"` |

#### 16.10.4 `ServiceCollectionIntegrationTests`

Arquivo `tests/QuickNET.Tests/Integration/DIIntegrationTests.cs`:

| Método | Descrição |
|---|---|
| `AllServices_Resolve_WithoutError` | `BuildServiceProvider()` → resolver `ReplEngine`, `ThemeService`, `CompletionEngine`, `InputHistoryService`, `MainWindowViewModel` — nenhum lança exceção |
| `MetaCommandService_HasAllDependencies` | Resolver `MetaCommandService` → verificar que `ThemeService` foi injetado (teste indireto via `/theme`) |

### 16.11 Verificação de regressão

Executar `dotnet test` e verificar que **todos** os 52 testes existentes continuam passando sem falhas. A adição de novos parâmetros nos construtores (`MainWindowViewModel`, `MetaCommandService`) não deve quebrar testes existentes — os testes precisam ser atualizados para prover as novas dependências.

**Ajustes necessários nos testes existentes:**

| Arquivo de teste | Ajuste |
|---|---|
| `MainWindowViewModelTests.cs` | Construtor do ViewModel agora requer `ThemeService`, `CompletionEngine`, `InputHistoryService`. Instanciar com stubs reais (são leves) ou criar instâncias mínimas. |
| `MetaCommandServiceTests.cs` | Construtor do `MetaCommandService` agora requer `ThemeService`. Instanciar `new ThemeService(_sessionState)`. |

### 16.12 Atualizar `AGENTS.md` (se aplicável)

Verificar se `AGENTS.md` precisa de atualização para refletir os novos serviços e diretórios. Adicionar a `Completion/` e `Theme/` na estrutura do Core.

---

## Acceptance Criteria

- [ ] `dotnet build` compila toda a solution sem erros.
- [ ] `ThemeService`, `CompletionEngine`, `InputHistoryService` registrados como singletons no DI.
- [ ] `MainWindowViewModel` aceita todas as novas dependências via construtor.
- [ ] `MetaCommandService` aceita `ThemeService` via construtor.
- [ ] `App.axaml.cs` aplica o tema inicial e reage a `ThemeChanged`.
- [ ] `MainWindow.axaml` contém o `Popup` de completion overlay.
- [ ] `/theme light`, `/theme dark`, `/theme system` funcionam end-to-end com hot-reload.
- [ ] Autocomplete popup aparece e funciona com `Ctrl+Space` e triggers automáticos.
- [ ] Navegação de histórico ↑↓ funciona e coexiste com o autocomplete.
- [ ] `dotnet test` executa **todos** os testes (existentes 52 + novos) sem falhas.
- [ ] Nenhum teste existente quebrou com as novas dependências.
- [ ] Cobertura global mantém >= 70%.

---

## Notes for AI Agent

- **Ordem de implementação:** TASKS-13 → TASKS-14 → TASKS-15 → TASKS-16. Cada bloco é independente em seu domínio (Core/App), mas TASKS-16 precisa de todos os anteriores completos.
- **Atualização de testes existentes:** A principal fonte de quebra é a adição de parâmetros nos construtores. `MainWindowViewModelTests` e `MetaCommandServiceTests` precisam ser atualizados para passar as novas dependências. Como `ThemeService`, `CompletionEngine`, e `InputHistoryService` são classes concretas leves, usar instâncias reais nos testes.
- **CompletionEngine nos testes:** O `CompletionEngine` precisa de `AssemblyResolutionService`. Nos testes, usar a instância real — ela é leve e não tem efeitos colaterais.
- **InputHistoryService nos testes:** Usar o construtor `internal` com path temporário. Cleanup no `[TestCleanup]`.
- **FluentTheme em App.axaml:** Se `<FluentTheme />` já está declarado em `App.axaml`, o código em `App.axaml.cs` deve encontrar essa instância existente via `app.Styles.OfType<FluentTheme>().FirstOrDefault()`. Se não encontrada, cria uma nova.
- **Verificar compatibilidade com versões do Avalonia:** `FluentThemeMode.System` está disponível no Avalonia 11.x+. Com a versão atual (12.0.3), confirmar que a API é suportada.
- **Nomes de arquivos e namespaces:** Seguir o padrão existente: file-scoped namespaces, PascalCase para tipos, camelCase para campos privados.
- **Commit separado:** Cada TASKS deve idealmente ser um commit separado para facilitar review e rollback.
