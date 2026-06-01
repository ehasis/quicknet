# TASKS-17: Signature Tooltip

**Block:** 17 de 17
**Depends on:** TASKS-14 (Autocomplete Engine & Popup UI), TASKS-11 (UI layout)
**PRD Reference:** `docs/PRD.md` — Seção 2.2 (US-11 Autocomplete, extensão para signature help)

---

## Objective

Implementar tooltip de assinatura de método/construtor que aparece ao digitar `(` ou `,`. O tooltip exibe a melhor sobrecarga detectada pelo Roslyn `SignatureHelpService`, com o parâmetro ativo destacado. Reutiliza o `AdhocWorkspace` já instanciado pelo `CompletionEngine` (novo método na mesma classe, sem nova classe de engine). Coexiste com o popup de autocomplete — tooltip abaixo do input, popup acima.

**Decisões de design:**
- Trigger: `(` e `,` (Roslyn decide se há contexto de invocação válido — método ou construtor)
- Tooltip **estático**: só atualiza ao digitar `(` ou `,`. Outros caracteres não o afetam.
- **Sem navegação** entre sobrecargas: apenas a melhor sobrecarga (Roslyn `SelectedItemIndex`)
- Posicionamento: `Placement="Bottom"` (abaixo do campo de texto)
- Coexistência: tooltip de assinatura e popup de autocomplete visíveis simultaneamente
- **Nenhuma nova classe de engine**: método `GetSignatureHelpAsync` adicionado ao `CompletionEngine` existente

---

## Tasks

### 17.1 Criar modelo `SignatureHelpSegment`

Arquivo `src/QuickNET.Core/Models/SignatureHelpSegment.cs`:

```csharp
namespace QuickNET.Models;

public sealed record SignatureHelpSegment(string Text, bool IsActiveParameter);
```

Segmentos individuais da assinatura formatada. `IsActiveParameter = true` marca o parâmetro que o usuário está preenchendo no momento, que será destacado visualmente (cor de destaque).

---

### 17.2 Adicionar `GetSignatureHelpAsync` ao `CompletionEngine`

Arquivo `src/QuickNET.Core/Completion/CompletionEngine.cs`:

Adicionar `using Microsoft.CodeAnalysis.SignatureHelp;`.

Adicionar o método público:

```csharp
public async Task<IReadOnlyList<SignatureHelpSegment>?> GetSignatureHelpAsync(
    string sourceCode,
    int cursorPosition,
    Language language,
    char triggerCharacter,
    IReadOnlyList<string>? extraReferences = null,
    IReadOnlyList<string>? extraImports = null,
    CancellationToken ct = default)
{
    ct.ThrowIfCancellationRequested();

    var (wrappedCode, adjustedPosition) = WrapForCompletion(sourceCode, cursorPosition, language);

    var workspace = GetOrCreateWorkspace(wrappedCode, language, extraReferences, extraImports);
    var project = workspace.CurrentSolution.Projects.FirstOrDefault();
    if (project is null) return null;

    var document = project.Documents.FirstOrDefault();
    if (document is null) return null;

    var sourceText = SourceText.From(wrappedCode);
    document = document.WithText(sourceText);

    var sigService = SignatureHelpService.GetService(document);
    if (sigService is null) return null;

    ct.ThrowIfCancellationRequested();

    var triggerReason = triggerCharacter == '('
        ? SignatureHelpTriggerReason.InvokeSignatureHelp
        : SignatureHelpTriggerReason.TypeCharCommand;

    var sigHelp = await sigService.GetSignatureHelpAsync(
        document, adjustedPosition,
        triggerReason, ct);

    if (sigHelp is null || sigHelp.Items.IsEmpty) return null;

    var bestItem = sigHelp.Items[sigHelp.SelectedItemIndex];
    var activeParamIndex = sigHelp.ArgumentIndex;

    var segments = new List<SignatureHelpSegment>();

    foreach (var part in bestItem.PrefixDisplayParts)
        segments.Add(new SignatureHelpSegment(part.ToString(), false));

    for (int i = 0; i < bestItem.Parameters.Length; i++)
    {
        var param = bestItem.Parameters[i];
        if (i > 0)
        {
            foreach (var sep in bestItem.SeparatorDisplayParts)
                segments.Add(new SignatureHelpSegment(sep.ToString(), false));
        }
        foreach (var part in param.LabelDisplayParts)
            segments.Add(new SignatureHelpSegment(part.ToString(), i == activeParamIndex));
    }

    foreach (var part in bestItem.SuffixDisplayParts)
        segments.Add(new SignatureHelpSegment(part.ToString(), false));

    return segments;
}
```

**Notas:**
- Reutiliza `WrapForCompletion` (idêntico ao usado para completions — o wrapping de corpo de método funciona para signature help).
- Reutiliza `GetOrCreateWorkspace` (usa o workspace já cacheado).
- `SignatureHelpService.GetService(document)` retorna o serviço específico da linguagem (C# ou VB).
- `triggerCharacter` determina `SignatureHelpTriggerReason`: `(` → `InvokeSignatureHelp`, `,` → `TypeCharCommand`.
- `sigHelp.SelectedItemIndex` é o índice da melhor sobrecarga calculada pelo Roslyn.
- `sigHelp.ArgumentIndex` indica qual parâmetro está ativo (baseado nos argumentos já digitados).
- Os segmentos preservam a ordenação: `PrefixDisplayParts` (return type + method name + `(`) → parâmetros (com `SeparatorDisplayParts` entre eles) → `SuffixDisplayParts` (`)` + sufixos).
- O parâmetro ativo (`i == activeParamIndex`) tem `IsActiveParameter = true`.

---

### 17.3 Adicionar trigger helper para signature help

Arquivo `src/QuickNET.App/Completion/TriggerHelper.cs`:

Adicionar método:

```csharp
public static bool ShouldTriggerSignatureHelp(string text, int position)
{
    if (position <= 0 || position > text.Length) return false;
    return text[position - 1] == '(' || text[position - 1] == ',';
}
```

---

### 17.4 Criar `SignatureHelpViewModel`

Arquivo `src/QuickNET.App/ViewModels/SignatureHelpViewModel.cs`:

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using QuickNET.Models;

namespace QuickNET.App.ViewModels;

public partial class SignatureHelpViewModel : ObservableObject
{
    [ObservableProperty]
    private string _signatureText = "";

    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private int _activeParameterStart;

    [ObservableProperty]
    private int _activeParameterLength;

    public void Show(IReadOnlyList<SignatureHelpSegment> segments)
    {
        var sb = new System.Text.StringBuilder();
        int activeStart = -1;
        int activeLength = 0;

        foreach (var seg in segments)
        {
            if (seg.IsActiveParameter)
            {
                activeStart = sb.Length;
                sb.Append(seg.Text);
                activeLength = sb.Length - activeStart;
            }
            else
            {
                sb.Append(seg.Text);
            }
        }

        SignatureText = sb.ToString();
        ActiveParameterStart = activeStart;
        ActiveParameterLength = activeLength;
        IsVisible = true;
    }

    public void Hide()
    {
        IsVisible = false;
        SignatureText = "";
        ActiveParameterStart = -1;
        ActiveParameterLength = 0;
    }
}
```

**Notas:**
- O ViewModel recebe a lista de `SignatureHelpSegment` e monta a string completa.
- `ActiveParameterStart` / `ActiveParameterLength` delimitam a região a ser destacada na UI.
- O controle `SignatureTooltip` usa esses valores para renderizar o `TextBlock` com Inlines coloridos.

---

### 17.5 Criar `SignatureTooltip` (UserControl)

Arquivo `src/QuickNET.App/Controls/SignatureTooltip.axaml`:

```xml
<UserControl xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:vm="clr-namespace:QuickNET.App.ViewModels"
             x:Class="QuickNET.App.Controls.SignatureTooltip"
             x:DataType="vm:SignatureHelpViewModel">
    <Border Background="{DynamicResource SystemControlBackgroundChromeMediumBrush}"
            BorderBrush="{DynamicResource SystemControlForegroundChromeDisabledBrush}"
            BorderThickness="1" CornerRadius="4"
            BoxShadow="0 2 8 rgba(0,0,0,0.2)"
            Padding="6,3" MaxWidth="700">
        <TextBlock x:Name="SignatureTextBlock"
                   FontFamily="Cascadia Code, Consolas, monospace"
                   FontSize="12"
                   TextWrapping="NoWrap"
                   TextTrimming="CharacterEllipsis" />
    </Border>
</UserControl>
```

Arquivo `src/QuickNET.App/Controls/SignatureTooltip.axaml.cs`:

```csharp
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace QuickNET.App.Controls;

public partial class SignatureTooltip : UserControl
{
    public SignatureTooltip()
    {
        InitializeComponent();
    }

    public void UpdateSignature(string text, int activeStart, int activeLength)
    {
        var inlines = SignatureTextBlock.Inlines;
        inlines.Clear();

        if (string.IsNullOrEmpty(text))
            return;

        var defaultBrush = (IBrush?)this.FindResource("SystemControlForegroundBaseHighBrush")
            ?? Brushes.White;
        var accentBrush = (IBrush?)this.FindResource("SystemControlForegroundAccentBrush")
            ?? Brushes.DodgerBlue;

        if (activeStart < 0 || activeLength <= 0)
        {
            inlines.Add(new Run(text) { Foreground = defaultBrush });
            return;
        }

        if (activeStart > 0)
            inlines.Add(new Run(text[..activeStart]) { Foreground = defaultBrush });

        inlines.Add(new Run(text.Substring(activeStart, activeLength)) { Foreground = accentBrush });

        var afterEnd = activeStart + activeLength;
        if (afterEnd < text.Length)
            inlines.Add(new Run(text[afterEnd..]) { Foreground = defaultBrush });
    }
}
```

---

### 17.6 Integrar signature help no `MainWindowViewModel`

Arquivo `src/QuickNET.App/ViewModels/MainWindowViewModel.cs`:

Adicionar campos (no início da classe, junto aos existentes):

```csharp
private CancellationTokenSource? _signatureCts;
private DispatcherTimer? _signatureDebounceTimer;
```

Adicionar propriedade:

```csharp
public SignatureHelpViewModel SignatureHelp { get; } = new();
```

Adicionar método `RequestSignatureHelp`:

```csharp
public void RequestSignatureHelp(string code, int cursorPosition)
{
    _signatureCts?.Cancel();
    _signatureCts?.Dispose();
    _signatureCts = null;

    _signatureDebounceTimer?.Stop();
    _signatureDebounceTimer = new DispatcherTimer
    {
        Interval = TimeSpan.FromMilliseconds(150)
    };
    _signatureDebounceTimer.Tick += async (_, _) =>
    {
        _signatureDebounceTimer.Stop();
        await FetchSignatureHelp(code, cursorPosition);
    };
    _signatureDebounceTimer.Start();
}
```

Adicionar método `FetchSignatureHelp`:

```csharp
private async Task FetchSignatureHelp(string code, int cursorPosition)
{
    _signatureCts = new CancellationTokenSource();
    var ct = _signatureCts.Token;

    // Determinar o caractere trigger (último antes do cursor)
    var triggerChar = cursorPosition > 0 && cursorPosition <= code.Length
        ? code[cursorPosition - 1]
        : '\0';
    if (triggerChar != '(' && triggerChar != ',')
        return;

    try
    {
        var language = SelectedLanguageIndex == 0 ? Language.CSharp : Language.VisualBasic;
        var segments = await _completionEngine.GetSignatureHelpAsync(
            code, cursorPosition, language, triggerChar,
            _sessionState.ExtraReferences, _sessionState.ExtraImports, ct);

        if (!ct.IsCancellationRequested)
        {
            if (segments is not null && segments.Count > 0)
                SignatureHelp.Show(segments);
            else
                SignatureHelp.Hide();
        }
    }
    catch (OperationCanceledException) { }
    catch (Exception) { SignatureHelp.Hide(); }
}
```

---

### 17.7 Atualizar `MainWindow.axaml` — adicionar popup de signature

Arquivo `src/QuickNET.App/Views/MainWindow.axaml`:

Adicionar o segundo `Popup` após o `CompletionOverlay` existente (linha 74):

```xml
        <Popup x:Name="SignatureOverlay"
               IsOpen="{Binding SignatureHelp.IsVisible}"
               Placement="Bottom"
               HorizontalOffset="0" VerticalOffset="4"
               Grid.RowSpan="3">
            <controls:SignatureTooltip x:Name="SignatureTooltipControl"
                                        DataContext="{Binding SignatureHelp}" />
        </Popup>
    </Grid>
```

---

### 17.8 Atualizar `MainWindow.axaml.cs` — handlers de signature help

Arquivo `src/QuickNET.App/Views/MainWindow.axaml.cs`:

#### 17.8.1 Setar `PlacementTarget` do signature popup

Em `OnDataContextChanged` (após a linha 154 `CompletionOverlay.PlacementTarget = InputBox;`):

```csharp
SignatureOverlay.PlacementTarget = InputBox;
```

#### 17.8.2 Atualizar handler `TextChanged`

Substituir o bloco atual (linhas 168-179) por:

```csharp
            var isSignatureTrigger = TriggerHelper.ShouldTriggerSignatureHelp(text, pos);

            // Signature help — independente do completion
            if (isSignatureTrigger)
            {
                vm.RequestSignatureHelp(text, pos);
            }

            // Completion
            if (vm.Completion.IsVisible)
            {
                vm.RequestCompletions(text, pos);
            }
            else if (TriggerHelper.ShouldAutoTrigger(text, pos))
            {
                vm.RequestCompletions(text, pos);
            }
            else
            {
                vm.Completion.Hide();
            }
```

**Nota:** O tooltip de assinatura é estático — se `isSignatureTrigger` for `false` e o tooltip estiver visível, nada acontece (nem hide nem refresh). Só fecha via Escape.

#### 17.8.3 Atualizar `OnKeyDown` — Escape fecha ambos

No handler `OnKeyDown`, caso `Escape` (linhas 104-107), adicionar hide do signature:

```csharp
                case Key.Escape:
                    vm.Completion.Hide();
                    vm.SignatureHelp.Hide();
                    e.Handled = true;
                    return;
```

Adicionar também caso Escape quando o popup de completion **não** está visível mas o signature está:

```csharp
        if (e.Key == Key.Escape)
        {
            if (vm.SignatureHelp.IsVisible)
            {
                vm.SignatureHelp.Hide();
                e.Handled = true;
                return;
            }
        }
```

Inserir este bloco **antes** do `if (e.Key == Key.Enter)` no final do método (antes da linha 111).

#### 17.8.4 Atualizar signature tooltip quando ViewModel mudar

Em `OnDataContextChanged`, após configurar os eventos existentes, assinar mudanças nas propriedades do `SignatureHelp` para atualizar o controle:

```csharp
        vm.SignatureHelp.PropertyChanged += (s, args) =>
        {
            if (args.PropertyName is nameof(SignatureHelpViewModel.SignatureText)
                or nameof(SignatureHelpViewModel.ActiveParameterStart)
                or nameof(SignatureHelpViewModel.ActiveParameterLength))
            {
                SignatureTooltipControl.UpdateSignature(
                    vm.SignatureHelp.SignatureText,
                    vm.SignatureHelp.ActiveParameterStart,
                    vm.SignatureHelp.ActiveParameterLength);
            }
        };
```

**Nota:** O `PropertyChanged` do CommunityToolkit.Mvvm dispara para propriedades geradas via `[ObservableProperty]`. A assinatura é feita no `OnDataContextChanged` porque o DataContext pode ser reatribuído.

---

### 17.9 Criar testes de unidade

#### 17.9.1 Testes do `GetSignatureHelpAsync`

Arquivo `src/QuickNET.Tests/Completion/CompletionEngineTests.cs` — adicionar métodos (ou criar arquivo separado `SignatureHelpEngineTests.cs`):

| Método | Descrição |
|---|---|
| `GetSignatureHelp_MethodCall_ReturnsSignature` | Código `"Math.Max("`, cursor após `(`, C# → retorna segmentos com assinatura de `Max` |
| `GetSignatureHelp_TwoArgs_HighlightsFirstParam` | Código `"Math.Max("`, cursor após `(`, trigger `(` → `IsActiveParameter = true` no primeiro parâmetro |
| `GetSignatureHelp_Comma_MovesToNextParam` | Código `"Math.Max(1,"`, cursor após `,`, trigger `,` → `IsActiveParameter = true` no segundo parâmetro |
| `GetSignatureHelp_Constructor_ReturnsSignature` | Código `"new List<int>("`, cursor após `(`, C# → retorna assinatura do construtor |
| `GetSignatureHelp_NoInvocation_ReturnsNull` | Código `"if ("`, cursor após `(` → retorna `null` (não é invocação) |
| `GetSignatureHelp_EmptyCode_ReturnsNull` | Código `""` → retorna `null` |
| `GetSignatureHelp_Cancelled_Throws` | Passar token cancelado → `OperationCanceledException` |
| `GetSignatureHelp_RespectsLanguage_VB` | Código VB `"Math.Max("`, cursor após `(` → retorna assinatura VB |
| `GetSignatureHelp_WorkspaceReused` | Duas chamadas com mesmos refs → workspace não recriado (verificar indiretamente via performance) |

**Setup do teste:**
- Dependência: `AssemblyResolutionService` real + `CompletionEngine`
- Métodos de teste devem ser `async Task`
- Usar `CancellationToken.None` (exceto no teste de cancelamento)
- `Assert.IsNotNull(segments)` para verificar retorno
- `Assert.IsTrue(segments.Any(s => s.IsActiveParameter))` para verificar destaque

#### 17.9.2 Testes do `TriggerHelper`

Arquivo `src/QuickNET.Tests/Completion/CompletionTriggerTests.cs` — adicionar métodos:

| Método | Descrição |
|---|---|
| `ShouldTriggerSignatureHelp_OpenParen_ReturnsTrue` | `"Foo("`, pos 4 → `true` |
| `ShouldTriggerSignatureHelp_Comma_ReturnsTrue` | `"Foo(1,"`, pos 6 → `true` |
| `ShouldTriggerSignatureHelp_OtherChar_ReturnsFalse` | `"Foo."`, pos 4 → `false` |
| `ShouldTriggerSignatureHelp_EmptyString_ReturnsFalse` | `""`, pos 0 → `false` |
| `ShouldTriggerSignatureHelp_PositionZero_ReturnsFalse` | `"("`, pos 0 → `false` |

#### 17.9.3 Testes do `SignatureHelpViewModel`

Arquivo `src/QuickNET.Tests/ViewModels/SignatureHelpViewModelTests.cs`:

| Método | Descrição |
|---|---|
| `Show_WithSegments_SetsIsVisible` | `Show([...])` → `IsVisible == true` |
| `Show_WithSegments_SetsSignatureText` | `Show(segments)` → `SignatureText` contém texto concatenado |
| `Show_WithActiveParam_SetsBounds` | Segmento com `IsActiveParameter = true` → `ActiveParameterStart >= 0`, `ActiveParameterLength > 0` |
| `Hide_ClearsState` | `Show` → `Hide` → `IsVisible == false`, `SignatureText == ""` |
| `Show_WithoutActiveParam_HasNegativeStart` | Nenhum segmento ativo → `ActiveParameterStart == -1` |

---

### 17.10 Atualizar `AGENTS.md`

Adicionar ao `Architecture` tree:

```
  ├── Completion/       -- CompletionEngine, SignatureHelp (method within CompletionEngine)
```

E adicionar ao Tasks tracker:

```
- TASKS-17 (signature tooltip) — in_progress
```

---

## Acceptance Criteria

- [ ] `CompletionEngine.GetSignatureHelpAsync()` retorna assinatura formatada para chamadas de método e construtores.
- [ ] Tooltip aparece ao digitar `(` em contexto de invocação (ex: `Math.Max(`, `new List<int>(`).
- [ ] Tooltip **não** aparece para `(` fora de contexto de invocação (ex: `if (`, `while (`).
- [ ] Ao digitar `,` o tooltip é atualizado e o parâmetro ativo avança.
- [ ] Parâmetro ativo é destacado visualmente (cor de destaque).
- [ ] Tooltip posicionado abaixo do campo de texto (`Placement="Bottom"`).
- [ ] Tooltip e popup de autocomplete coexistem visíveis simultaneamente.
- [ ] Tooltip é estático: não atualiza nem fecha ao digitar outros caracteres.
- [ ] `Escape` fecha o tooltip.
- [ ] Nenhum novo registro no DI necessário (reutiliza `CompletionEngine`).
- [ ] `dotnet build` compila toda a solution sem erros.
- [ ] `dotnet test` executa todos os testes novos + existentes sem falhas.

---

## Notes for AI Agent

- **Sem nova classe de engine:** O método `GetSignatureHelpAsync` vai dentro do `CompletionEngine` existente. Isso reutiliza `WrapForCompletion`, `GetOrCreateWorkspace`, e o `AdhocWorkspace` já instanciado. Nenhuma duplicação.
- **API do Roslyn 5.3.0:** `SignatureHelpService` está em `Microsoft.CodeAnalysis.SignatureHelp` (já incluso no pacote `Microsoft.CodeAnalysis.Features`). A chamada esperada é `GetSignatureHelpAsync(document, position, triggerReason, cancellationToken)` ou `GetSignatureHelpAsync(document, position, triggerOptions, triggerReason, cancellationToken)`. Confirmar a assinatura exata no momento da implementação e ajustar se necessário.
- **`SignatureHelpTriggerReason`:** `InvokeSignatureHelp` para `(`, `TypeCharCommand` para `,`.
- **`SignatureHelpItem` display parts:** `PrefixDisplayParts` contém return type + method name + `(`. `SeparatorDisplayParts` contém `, ` entre parâmetros. `SuffixDisplayParts` contém `)` e sufixos. `Parameters[i].LabelDisplayParts` contém o tipo + nome do parâmetro i.
- **Debounce 150ms:** O debounce do signature help é mais curto que o do completion (300ms) para dar sensação de resposta imediata ao digitar `(`.
- **Coexistência:** Os dois sistemas (completion e signature) são independentes no `TextChanged` handler. Ambos podem estar visíveis ao mesmo tempo. Um não afeta o outro.
- **Estático:** O tooltip NÃO é atualizado nem fechado em keystrokes que não sejam `(` ou `,`. Apenas `Escape` ou execução de código (`ExecuteCode`) o remove. Isso é intencional — o usuário pediu o comportamento mais simples possível.
- **`PropertyChanged` no code-behind:** Para atualizar os Inlines do `SignatureTooltip`, o `MainWindow.axaml.cs` assina `vm.SignatureHelp.PropertyChanged`. Isso é feito no `OnDataContextChanged`. Certificar de remover a assinatura anterior se o DataContext mudar (guardar o handler num campo e fazer `-=` antes de `+=`).
- **Testes:** `CompletionEngineTests` existente já cobre completions. Os novos testes de signature help podem ser adicionados ao mesmo arquivo ou em arquivo separado `SignatureHelpEngineTests.cs`. Preferir arquivo separado para isolamento. O setup é idêntico (mesmo `AssemblyResolutionService` + `CompletionEngine`).
- **Namespace dos controles:** O `SignatureTooltip` deve usar namespace `QuickNET.App.Controls` (consistente com `CompletionPopup`).
- **Sem `[TestInitialize]`:** Seguir a convenção MSTest do projeto (construtor com DI, sem `[TestInitialize]`).
- **File-scoped namespaces:** Todos os novos arquivos devem usar file-scoped namespaces.
- **Commit separado:** TASKS-17 deve ser um commit separado.
