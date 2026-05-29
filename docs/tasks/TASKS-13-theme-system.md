# TASKS-13: Theme System

**Block:** 13 de 16
**Depends on:** TASKS-8 (SessionState, MetaCommandService), TASKS-11 (UI layout sem toolbar)
**PRD Reference:** `docs/PRD.md` — Seções 5.5 (Session State), 5.8 (Theme Engine), 2.2 (US-10)

---

## Objective

Implementar o sistema de temas (claro, escuro, alto contraste) com hot-reload via FluentTheme nativo do Avalonia, meta-comando `/theme`, detecção do tema do sistema operacional, e persistência em `SessionSettings`. Incluir testes unitários para o ThemeService, `/theme` meta-command, e persistência.

---

## Tasks

### 13.1 Adicionar campo `Theme` ao `SessionSettings`

Arquivo `src/QuickNET.Core/Models/SessionSettings.cs`:

Adicionar a propriedade:

```csharp
public string Theme { get; set; } = "System";  // "System", "Light", "Dark"
```

O valor padrão é `"System"` — o tema segue o SO por padrão.

### 13.2 Atualizar `SessionState` para expor `CurrentTheme`

Arquivo `src/QuickNET.Core/Session/SessionState.cs`:

Adicionar:

```csharp
public string CurrentTheme
{
    get => _settings.Theme;
    set
    {
        _settings.Theme = value;
        Save();
    }
}
```

- Seguir o mesmo padrão das demais propriedades (getter do `_settings`, setter com `Save()`).
- **Não** usar enum `AppTheme` no `SessionState` — manter como string para manter simplicidade na serialização. O enum é usado apenas pelo `ThemeService`.

### 13.3 Criar `AppTheme` enum e `ThemeService`

Criar diretório `src/QuickNET.Core/Theme/` e os arquivos:

**`src/QuickNET.Core/Theme/AppTheme.cs`:**

```csharp
namespace QuickNET.Theme;

public enum AppTheme
{
    System,
    Light,
    Dark
}
```

**`src/QuickNET.Core/Theme/ThemeService.cs`:**

```csharp
namespace QuickNET.Theme;

public class ThemeService
{
    private readonly SessionState _sessionState;

    public ThemeService(SessionState sessionState)
    {
        _sessionState = sessionState;
    }

    public AppTheme CurrentTheme
    {
        get => ParseTheme(_sessionState.CurrentTheme);
        set
        {
            _sessionState.CurrentTheme = value switch
            {
                AppTheme.Light => "Light",
                AppTheme.Dark => "Dark",
                _ => "System"
            };
            ThemeChanged?.Invoke(this, value);
        }
    }

    public event EventHandler<AppTheme>? ThemeChanged;

    public static AppTheme ParseTheme(string theme)
    {
        return theme?.ToLowerInvariant() switch
        {
            "light" => AppTheme.Light,
            "dark" => AppTheme.Dark,
            _ => AppTheme.System
        };
    }

    public static AppTheme DetectSystemTheme()
    {
        // No Windows, detecta o tema via registro ou SystemParameters
        // Se o Windows está em modo de alto contraste, FluentTheme lida nativamente
        try
        {
            var key = Microsoft.Win32.Registry.CurrentUser
                .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var appsUseLightTheme = key?.GetValue("AppsUseLightTheme");
            return appsUseLightTheme is 0 ? AppTheme.Dark : AppTheme.Light;
        }
        catch
        {
            return AppTheme.Light; // fallback
        }
    }
}
```

- `ThemeChanged` event permite que a camada de UI reaja a mudanças de tema.
- `DetectSystemTheme()` usa o registro do Windows para detectar o tema. Se falhar, fallback para `Light`.
- O serviço **não** depende do Avalonia — é puro .NET. A aplicação do tema na UI é responsabilidade da camada App.

### 13.4 Implementar `/theme` meta-comando no `MetaCommandService`

Arquivo `src/QuickNET.Core/MetaCommands/MetaCommandService.cs`:

Adicionar ao switch de comandos:

```csharp
"theme" => ExecuteTheme(args),
```

Implementar `ExecuteTheme(string? args)`:

- **Sem argumentos:** exibe o tema atual. Ex.: `"Current theme: System"`
- **Argumento válido** (`light`, `dark`, `system`): define o tema via `_themeService.CurrentTheme`, retorna `Success = true`. Ex.: `"Theme set to Dark."`
- **Argumento inválido:** retorna `Success = false`. Ex.: `"Invalid theme 'blue'. Valid values: light, dark, system."`

**Novas dependências no construtor:**

Adicionar `ThemeService` como dependência do `MetaCommandService`:

```csharp
public class MetaCommandService
{
    private readonly SessionState _sessionState;
    private readonly AssemblyResolutionService _assemblyResolver;
    private readonly ThemeService _themeService;  // nova

    public MetaCommandService(
        SessionState sessionState,
        AssemblyResolutionService assemblyResolver,
        ThemeService themeService)  // novo parâmetro
    { ... }
}
```

Atualizar também o método `ExecuteHelp()` para incluir `/theme` na lista de comandos exibidos.

### 13.5 Registrar `ThemeService` no DI

Arquivo `src/QuickNET.Core/ServiceCollectionExtensions.cs`:

```csharp
services.AddSingleton<ThemeService>();
```

O `MetaCommandService` já está registrado como singleton — o DI automaticamente injeta o novo `ThemeService` no construtor.

### 13.6 Aplicar tema na UI (Avalonia App)

Arquivo `src/QuickNET.App/App.axaml.cs`:

1. No construtor, resolver `ThemeService` do `IServiceProvider`.
2. Assinar `ThemeService.ThemeChanged`.
3. Na inicialização (`OnFrameworkInitializationCompleted`), aplicar o tema inicial:

```csharp
private void ApplyTheme(AppTheme theme)
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
        AppTheme.System => FluentThemeMode.System  // detecta SO + alto contraste
    };
}
```

- Quando `Theme` é `System`, o `FluentThemeMode.System` do Avalonia automaticamente detecta o tema do Windows **e** o modo de alto contraste — não é necessário código adicional.
- A troca é imediata (hot-reload): a UI inteira reflete a mudança sem reinicialização.

### 13.7 Atualizar `SessionInfoText` no ViewModel para incluir tema

Arquivo `src/QuickNET.App/ViewModels/MainWindowViewModel.cs`:

Atualizar `SessionInfoText` para incluir o tema:

```csharp
// Adicionar antes de C# | Timeout...
var themeLabel = _themeService.CurrentTheme switch
{
    AppTheme.Light => "☀",
    AppTheme.Dark => "☾",
    _ => ""  // System: não mostra label ou mostra "Auto"
};

// Resultado final ex.: "☾ C# | Timeout: 30s | Refs: 0 | Imports: 0"
```

Injetar `ThemeService` no construtor do `MainWindowViewModel` e armazenar como `_themeService`.

Notificar `SessionInfoText` após execução de `/theme` — já acontece via `ExecuteMetaCommand` que chama `OnPropertyChanged(nameof(SessionInfoText))`.

### 13.8 Restaurar tema ao iniciar a aplicação

Arquivo `src/QuickNET.App/App.axaml.cs`:

No `OnFrameworkInitializationCompleted`, após resolver o `ThemeService`:

```csharp
var themeService = _serviceProvider.GetRequiredService<ThemeService>();
if (themeService.CurrentTheme == AppTheme.System)
{
    var detected = ThemeService.DetectSystemTheme();
    // Aplica FluentThemeMode.System — o Avalonia gerencia a detecção
    // Se o usuário já tiver definido Light ou Dark manualmente, aplica esse valor
}
ApplyTheme(themeService.CurrentTheme);
```

### 13.9 Criar testes do Theme System

#### 13.9.1 `SessionSettingsThemeTests`

Arquivo `tests/QuickNET.Tests/Session/SessionSettingsThemeTests.cs`:

Classe: `[TestClass] sealed`.

| Método | Descrição |
|---|---|
| `SessionSettings_DefaultTheme_IsSystem` | Novo `SessionSettings` → `Theme == "System"` |
| `SessionSettings_Deserialize_MissingTheme_DefaultsToSystem` | JSON sem campo `theme` → após desserializar, `Theme == "System"` |

**Nota:** Estes testes podem ser adicionados ao `SessionStateTests.cs` existente se preferir não criar um arquivo separado. No entanto, um arquivo separado mantém o isolamento temático.

#### 13.9.2 `ThemeServiceTests`

Arquivo `tests/QuickNET.Tests/Theme/ThemeServiceTests.cs` (criar diretório `Theme/`):

Classe: `[TestClass] sealed`. Construtor cria `SessionState` com path temporário e `ThemeService`.

| Método | Descrição |
|---|---|
| `Constructor_DefaultTheme_IsSystem` | SessionState com defaults → `ThemeService.CurrentTheme == AppTheme.System` |
| `SetTheme_Light_UpdatesSessionState` | `CurrentTheme = AppTheme.Light` → `SessionState.CurrentTheme == "Light"` |
| `SetTheme_Dark_UpdatesSessionState` | `CurrentTheme = AppTheme.Dark` → `SessionState.CurrentTheme == "Dark"` |
| `SetTheme_System_UpdatesSessionState` | `CurrentTheme = AppTheme.System` → `SessionState.CurrentTheme == "System"` |
| `SetTheme_FiresThemeChangedEvent` | Set `CurrentTheme = AppTheme.Dark` → evento `ThemeChanged` disparado com `AppTheme.Dark` |
| `SetTheme_SameValue_FiresEvent` | Set já estando no mesmo tema → evento disparado (idempotente) |
| `ParseTheme_Light_ReturnsLight` | `ParseTheme("Light")` → `AppTheme.Light` |
| `ParseTheme_Dark_ReturnsDark` | `ParseTheme("dark")` (lowercase) → `AppTheme.Dark` |
| `ParseTheme_Invalid_ReturnsSystem` | `ParseTheme("blue")` → `AppTheme.System` |
| `ParseTheme_Null_ReturnsSystem` | `ParseTheme(null)` → `AppTheme.System` |
| `ParseTheme_Empty_ReturnsSystem` | `ParseTheme("")` → `AppTheme.System` |
| `DetectSystemTheme_DoesNotThrow` | `DetectSystemTheme()` retorna `AppTheme.Light` ou `AppTheme.Dark`, não lança exceção |

**Nota:** `DetectSystemTheme()` depende do registro do Windows. O teste básico verifica que não lança exceção e retorna um valor válido. Em CI sem registro, cai no catch e retorna `Light`. Isso é aceitável.

#### 13.9.3 `MetaCommandServiceThemeTests`

Adicionar ao arquivo existente `tests/QuickNET.Tests/MetaCommands/MetaCommandServiceTests.cs`:

| Método | Descrição |
|---|---|
| `Execute_Theme_Light_SetsTheme` | `/theme light` → `Success == true`, `SessionState.CurrentTheme == "Light"` |
| `Execute_Theme_Dark_SetsTheme` | `/theme dark` → `Success == true`, `SessionState.CurrentTheme == "Dark"` |
| `Execute_Theme_System_SetsTheme` | `/theme system` → `Success == true`, `SessionState.CurrentTheme == "System"` |
| `Execute_Theme_CaseInsensitive_SetsTheme` | `/theme DARK` → `Success == true` |
| `Execute_Theme_NoArgs_ShowsCurrent` | `/theme` sem args → `DisplayText` contém tema atual |
| `Execute_Theme_InvalidArg_ReturnsError` | `/theme blue` → `Success == false`, contém "Invalid theme" e "Valid values" |

**Nota:** O construtor do `MetaCommandServiceTests` precisa ser atualizado para injetar `ThemeService`. Como o `ThemeService` depende de `SessionState`, e o `SessionState` já é criado com path temporário no setup, basta adicionar:

```csharp
_themeService = new ThemeService(_sessionState);
_metaService = new MetaCommandService(_sessionState, _assemblyResolver, _themeService);
```

#### 13.9.4 `MainWindowViewModelThemeTests`

Adicionar ao arquivo existente `tests/QuickNET.Tests/ViewModels/MainWindowViewModelTests.cs`:

| Método | Descrição |
|---|---|
| `SessionInfoText_DefaultTheme_ContainsSystem` | Tema padrão → `SessionInfoText` não contém ícone de light/dark |
| `SessionInfoText_DarkTheme_ContainsDarkIndicator` | Após `/theme dark` → `SessionInfoText` contém indicador de dark |
| `SessionInfoText_AfterThemeMetaCommand_Updates` | Executar `/theme light` → `SessionInfoText` atualizado |

---

## Acceptance Criteria

- [ ] `SessionSettings` contém propriedade `Theme` com default `"System"`.
- [ ] `ThemeService` expõe `CurrentTheme` (enum `AppTheme`), `ThemeChanged` event, `ParseTheme()` e `DetectSystemTheme()`.
- [ ] `/theme light`, `/theme dark`, `/theme system` funcionam corretamente.
- [ ] `/theme` sem argumentos exibe o tema atual.
- [ ] `/theme` com argumento inválido exibe erro descritivo.
- [ ] A troca de tema é imediata (hot-reload) via `FluentThemeMode` do Avalonia.
- [ ] Tema `System` detecta automaticamente o tema do Windows (incluindo alto contraste).
- [ ] A preferência de tema persiste em `settings.json` entre reinicializações.
- [ ] `ThemeService` registrado como singleton no DI.
- [ ] `SessionInfoText` reflete o tema atual.
- [ ] `dotnet test` executa todos os testes (existentes + novos) sem falhas.
- [ ] Cobertura >= 70% nos novos módulos (`ThemeService`, `/theme` command).

---

## Notes for AI Agent

- **FluentThemeMode.System:** O Avalonia já gerencia a detecção de tema do SO (light/dark) e alto contraste quando `Mode = FluentThemeMode.System`. Não é necessário código adicional para alto contraste.
- **Registry access:** `DetectSystemTheme()` acessa o registro do Windows (`HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize`). Em ambientes sem essa chave (CI, Windows Server), o try/catch garante fallback para `Light`.
- **FluentTheme no App.axaml:** Verificar se o `App.axaml` já declara `<FluentTheme />` no `Application.Styles`. Se sim, o código em `App.axaml.cs` deve encontrar e modificar essa instância existente em vez de adicionar uma nova.
- **Avalonia.Diagnostics:** O pacote `Avalonia.Diagnostics` está na versão 11.3.17 (versão diferente dos demais pacotes Avalonia 12.0.3). Não "corrigir" essa discrepância.
- **Uso de emojis no SessionInfoText:** Os ícones ☀/☾ são sugestões. Se houver preocupação com renderização cross-platform (embora seja Windows-only), usar texto puro: `"Light"`, `"Dark"`.
- **InternalsVisibleTo:** Já configurado no projeto Core (`[assembly: InternalsVisibleTo("QuickNET.Tests")]`). O construtor `internal SessionState(string filePath)` continua acessível para testes.
- **Ordem de construção dos testes:** `ThemeService` depende de `SessionState`. Usar o mesmo padrão de path temporário dos testes existentes (`SessionStateTests`).
- **Evento `ThemeChanged`:** Disparado **após** o `SessionState` ser atualizado e salvo. A UI reage assincronamente.
- **Não usar `[CallerMemberName]` ou `[CallerFilePath]`** — manter compatível com o estilo existente.
