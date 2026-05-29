# TASKS-11: UI — Meta-command Routing & Status Bar

**Block:** 11 de 12
**Depends on:** TASKS-5 (UI shell), TASKS-6 (ViewModel), TASKS-8 (SessionState, MetaCommandService), TASKS-9, TASKS-10
**PRD Reference:** `docs/PRD.md` — Seções 4.1, 4.2, 5.4

---

## Objective

Simplificar a interface visual removendo os controles de toolbar (ComboBoxes de linguagem e timeout, botão Clear) — todo controle da sessão é feito via meta-comandos. Atualizar o ViewModel para rotear meta-comandos e a barra de status para exibir as informações relevantes da sessão (linguagem ativa, timeout, referências, imports).

---

## Tasks

### 11.1 Simplificar o layout — remover toolbar

Arquivo `src/QuickNET.App/Views/MainWindow.axaml`:

- Remover o StackPanel da toolbar (Grid.Row="0") que continha os ComboBoxes de linguagem e timeout e o botão Clear.
- Ajustar `RowDefinitions` de `Auto,*,Auto,Auto` para `*,Auto,Auto`.
- Atualizar `Grid.Row` dos elementos: conversation → 0, input → 1, status bar → 2.

### 11.2 Atualizar barra de status

Incluir no `SessionInfoText` (já existente) a linguagem ativa:

```
C# | Timeout: 30s | Refs: 0 | Imports: 0
```

### 11.3 Atualizar ViewModel — roteamento de meta-comandos

Arquivo `src/QuickNET.App/ViewModels/MainWindowViewModel.cs`:

- **Remover** `TimeoutOptions` array e `SelectedTimeoutIndex` (não há mais ComboBox de timeout).
- **Remover** `OnSelectedLanguageIndexChanged` e `OnSelectedTimeoutIndexChanged` (não há mais sync bidirecional com ComboBoxes).
- **Remover** `RestoreSessionSettings` — a restauração da linguagem é feita inline no construtor.
- **Manter** `SelectedLanguageIndex` para uso interno em `ExecuteCode`.
- **Manter** `/lang` sync em `ExecuteMetaCommand` para atualizar `SelectedLanguageIndex`.
- **Remover** `/timeout` sync em `ExecuteMetaCommand` (não há mais ComboBox).
- **Atualizar** `SessionInfoText` para incluir linguagem.

### 11.4 Verificar DI

`Program.cs` e `App.axaml.cs` não precisam de alterações — as dependências já estão registradas via `AddQuickNETCore()`.

---

## Acceptance Criteria

- [ ] A toolbar foi removida — não há ComboBoxes nem botão Clear na UI.
- [ ] A conversação ocupa todo o topo da janela.
- [ ] A barra de status exibe: `Ready | C# | Timeout: 30s | Refs: 0 | Imports: 0`.
- [ ] `/lang vb` altera a linguagem ativa e atualiza a barra de status.
- [ ] `/timeout 60` altera o timeout e atualiza a barra de status.
- [ ] `/clear` limpa o painel de conversação e o histórico.
- [ ] `/help` exibe a lista de comandos no painel.
- [ ] O pipeline normal de execução (código sem `/`) continua funcionando.

---

## Notes for AI Agent

- A toolbar foi totalmente removida — linguagem, timeout e clear são controlados exclusivamente via meta-comandos.
- `SelectedLanguageIndex` permanece como `[ObservableProperty]` para uso interno em `ExecuteCode()` e nos testes, mas não tem binding XAML.
- `ClearHistoryCommand` permanece no ViewModel mas sem binding XAML (útil para testes e possível uso futuro).
- `SessionInfoText` é uma propriedade calculada — seu valor é notificado manualmente via `OnPropertyChanged` após cada meta-command.
