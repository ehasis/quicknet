# QuickNET — Product Requirements Document

> **Versão:** 1.1 — Meta & Session  
> **Data:** 2026-05-29  
> **Status:** Draft

---

## 1. Executive Summary

### Problem Statement

Desenvolvedores .NET frequentemente precisam testar pequenos trechos de código, validar expressões ou prototipar algoritmos sem o overhead de criar um projeto completo, abrir o Visual Studio ou configurar um ambiente de script. As alternativas atuais (LinqPad, dotnet-script, csi) carecem de uma experiência unificada para C# e VB.NET com feedback imediato.

### Proposed Solution

O **QuickNET** é uma aplicação desktop leve que funciona como um REPL (Read-Eval-Print Loop) para C# e VB.NET, utilizando o Roslyn para compilação completa em memória. A interface segue o modelo conversacional (input/output encadeado), permitindo que o desenvolvedor digite instruções e receba resultados instantaneamente, com suporte a expressões simples e blocos multi-linha para pequenos algoritmos.

### Success Criteria

- **K1:** Executar 2 ou mais operações consecutivas sem crash.
- **K2:** Tempo de compilação + execução de uma expressão simples (ex.: `2 + 2`) em menos de 500ms.
- **K3:** Suporte a alternância entre C# e VB.NET sem reiniciar a aplicação.
- **K4:** Cobertura de testes unitários >= 70% nos módulos core (compilação, execução, parsing).
- **K5:** Meta-comandos (`/clear`, `/help`, `/reference`, `/import`, `/references`, `/imports`, `/timeout`, `/lang`) funcionam via input text sem interferir na execução normal de código.

---

## 2. User Experience & Functionality

### 2.1 User Personas

| Persona | Descrição |
|---|---|
| **Dev .NET Explorador** | Desenvolvedor que quer testar rapidamente um snippet, validar uma API do .NET ou explorar comportamento de bibliotecas. |
| **Estudante / Júnior** | Alguém a aprender C# ou VB.NET que usa o QuickNET como sandbox de experimentação. |
| **Arquiteto / Tech Lead** | Profissional que prototipa algoritmos ou valida abordagens antes de integrá-las a um codebase maior. |

### 2.2 User Stories

| ID | Story | Acceptance Criteria |
|---|---|---|
| **US-01** | Como desenvolvedor, quero digitar uma expressão simples (ex.: `2 + 2`) e ver o resultado imediatamente para validar comportamentos rápidos. | - Input single-line é processado após `Enter`.<br>- Resultado exibido no painel de output em < 500ms.<br>- Erros de compilação/runtime são reportados com localização (linha, coluna) quando disponível. |
| **US-02** | Como desenvolvedor, quero escrever blocos multi-linha (ex.: um `if` com várias instruções) para prototipar pequenos algoritmos. | - O input aceita múltiplas linhas.<br>- A submissão é feita via atalho (ex.: `Shift+Enter`).<br>- O bloco completo é compilado e executado como uma unidade. |
| **US-03** | Como desenvolvedor, quero alternar entre C# e VB.NET para testar código em ambas as linguagens. | - A troca de linguagem é feita via meta-comando `/lang cs` ou `/lang vb`.<br>- A troca de linguagem não reinicia a aplicação nem perde o histórico.<br>- Cada entrada do histórico preserva a linguagem com que foi executada. |
| **US-04** | Como desenvolvedor, quero ver o histórico de comandos executados e seus resultados para revisitar testes anteriores. | - Histórico exibido como conversação (input → output) em painel scrollável.<br>- Histórico persiste entre sessões (armazenamento local). |
| **US-05** | Como desenvolvedor, quero que erros sejam apresentados de forma clara para entender o que corrigir. | - Erros de compilação exibidos com mensagem do Roslyn e indicador de posição.<br>- Exceções de runtime exibidas com stack trace resumida. |
| **US-06** | Como desenvolvedor, quero usar meta-comandos via input (`/clear`, `/help`, `/lang`, etc.) para controlar a sessão sem usar o mouse. | - Comandos iniciados com `/` são interpretados como meta-comandos e não compilados.<br>- `/clear` limpa painel e histórico.<br>- `/help` lista todos os comandos disponíveis com descrição.<br>- `/lang cs` e `/lang vb` alternam a linguagem ativa. |
| **US-07** | Como desenvolvedor, quero adicionar referências a assemblies extras e namespaces para usar APIs que não estão no conjunto padrão. | - `/reference System.Text.Json` adiciona o assembly à compilação.<br>- `/import System.Text.Json` (alias `/using`) adiciona o namespace aos imports.<br>- `/references` lista assemblies referenciados.<br>- `/imports` lista namespaces importados.<br>- Referências e imports persistem entre execuções e entre reinicializações da aplicação. |
| **US-08** | Como desenvolvedor, quero configurar um timeout de execução para evitar que loops infinitos travem a aplicação. | - Timeout padrão de 30s.<br>- Timeout pode ser alterado via `/timeout <segundos>`.<br>- Execução é cancelada após o timeout com mensagem clara. |
| **US-09** | Como desenvolvedor, quero que minhas configurações de sessão (referências, imports, timeout, linguagem) sejam preservadas ao fechar e reabrir a aplicação. | - Configurações salvas automaticamente em `%APPDATA%\QuickNET\settings.json`.<br>- Carregadas ao iniciar a aplicação.<br>- Arquivo corrompido não causa crash (fallback para defaults). |

### 2.3 Acceptance Criteria (Gerais)

- A aplicação é distribuída como executável standalone (single-file) para Windows.
- A instalação é feita via instalador MSI/EXE ou ZIP extraível.
- A janela principal contém um painel scrollável de conversação (input → output) e um campo de input na parte inferior.
- O input suporta single-line (`Enter`) e multi-linha (`Shift+Enter`).
- A linguagem pode ser trocada via meta-comando `/lang cs` ou `/lang vb`.
- Erros de compilação e runtime são capturados e exibidos no painel de output.
- **v1.1:** Inputs iniciados com `/` são tratados como meta-comandos.
- **v1.1:** Meta-comandos exibem o resultado no painel de conversação (como qualquer output).
- **v1.1:** A barra de status exibe linguagem ativa, timeout, referências e imports.
- **v1.1:** Configurações de sessão (referências, imports, timeout, linguagem) persistem em `settings.json`.

### 2.4 Non-Goals (v1.1)

Os seguintes itens estão explicitamente fora do escopo da v1.1 e serão considerados para versões futuras:

- **Autocomplete / IntelliSense** — popup de sugestões durante a digitação.
- **Syntax highlighting** — fora do escopo; será introduzido de forma básica a partir da v1.2.
- **Compartilhamento de contexto entre execuções** — cada input é compilado e executado isoladamente (sem estado persistente de variáveis entre linhas). Referências e imports são a única exceção.
- **Modo CLI / Terminal** — adiado para v3.0.
- **Suporte a Linux e macOS** — Windows apenas na v1.x.
- **Debugger integrado**.
- **Suporte a scripts (.csx / .vbx)** para carregamento externo.
- **NuGet package import on-the-fly** — adiado para v2.0.
- **`/load` e `/reset`** — dispensados por baixa prioridade.

---

## 3. Technical Specifications

### 3.1 Architecture Overview

```
┌────────────────────────────────────────────────────────┐
│                   Avalonia UI                          │
│  ┌──────────────────────────────────────────────────┐  │
│  │              Conversation Panel                  │  │
│  └──────────────────────────────────────────────────┘  │
│  ┌──────────────────────────────────────────────────┐  │
│  │                   Input Field                    │  │
│  └──────────────────────────────────────────────────┘  │
└──────────────────────┬─────────────────────────────────┘
                       │
┌──────────────────────v─────────────────────────────────┐
│                 Core Engine (v1.1)                     │
│  ┌──────────────┐  ┌─────────────┐  ┌───────────────┐  │
│  │ MetaCmd      │  │  Compiler   │  │   Executor    │  │
│  │ Parser       │  │  (Roslyn)   │  │               │  │
│  └──────────────┘  └─────────────┘  └───────────────┘  │
│  ┌──────────────┐  ┌─────────────┐  ┌───────────────┐  │
│  │ SessionState │  │ Assembly    │  │ Timeout       │  │
│  │ (persisted)  │  │ Resolver    │  │ Manager       │  │
│  └──────────────┘  └─────────────┘  └───────────────┘  │
│  ┌──────────────┐  ┌─────────────┐                     │
│  │  History     │  │  Settings   │                     │
│  │  Manager     │  │  Manager    │                     │
│  └──────────────┘  └─────────────┘                     │
└────────────────────────────────────────────────────────┘
```

### 3.2 Technology Stack

| Componente | Tecnologia | Justificativa |
|---|---|---|
| **Runtime** | .NET 10 | Versão mais recente com melhorias de performance e AOT. |
| **UI Framework** | Avalonia UI | Cross-platform (prepara Linux/macOS futuro), XAML-based, maduro. |
| **Linguagem da aplicação** | C# 14 | Alinhado com .NET 10; sintaxe moderna. |
| **Compilador/Engine** | Microsoft.CodeAnalysis (Roslyn) v4.x | Compilação completa em memória com `CSharpCompilation` / `VisualBasicCompilation`. |
| **Testes** | MSTest | Preferência do time; integração nativa com .NET. |
| **Empacotamento** | MSI/EXE via WiX Toolset ou Velopack; ZIP manual | Distribuição no GitHub Releases. |

### 3.3 Compilation Model

O QuickNET usará **compilação completa do Roslyn** (não scripting API):

1. **Template de envoltório**: O código do usuário é envolvido num template de classe/método que permite capturar o valor de retorno da última expressão.
2. **Referências**: Um conjunto padrão de assemblies é referenciado (`System.dll`, `System.Core.dll`, `System.IO.dll`, `System.Linq.dll`, etc.).
3. **Compilação em memória**: O assembly é compilado em memória (sem arquivos .dll físicos).
4. **Isolamento via AssemblyLoadContext**: Cada execução utiliza um `AssemblyLoadContext` isolado e descartável, evitando vazamentos de memória e permitindo que assemblies sejam descarregados após o uso.
5. **Execução via Reflection**: O método gerado é invocado e o resultado é capturado e serializado para exibição.  
   **Nota:** A captura de `Console.WriteLine` deve ser feita **dentro** do template gerado (com `Console.SetOut` no próprio método `Execute()`), e não no processo host. O `AssemblyLoadContext` isola a identidade de tipos; o `System.Console` do host não é o mesmo `System.Console` do assembly carregado, mesmo com `Load(AssemblyName)` retornando `null`.

```
Usuário digita:  2 + 2

Template C#:
  using System;
  using System.IO;
  using System.Linq;
  // ... outras usings padrão
  
  public static class QuickNETSession {
      public static object Execute() {
          return 2 + 2;
      }
  }

Compilação → Assembly em memória → Invoke Execute() → Exibe "4"
```

### 3.4 Key Dependencies (NuGet Packages)

| Pacote | Propósito |
|---|---|
| `Microsoft.CodeAnalysis.CSharp` | Compilação C# |
| `Microsoft.CodeAnalysis.VisualBasic` | Compilação VB.NET |
| `Avalonia` + `Avalonia.Desktop` | Framework UI |
| `Avalonia.Themes.Fluent` | Tema Fluent (Windows 11 look) |
| `Avalonia.Diagnostics` | Dev tools (opcional) |
| `MSTest.Sdk` | Testes unitários |

### 3.5 Security & Privacy

- **Sandbox**: Nenhum — confiança total no usuário. Código arbitrário é executado com os mesmos privilégios do processo.
- **Acesso a disco/rede**: Irrestrito (por design — o usuário pode chamar `File.ReadAllText`, `HttpClient`, etc.).
- **Timeout**: Não implementado no MVP. Poderá ser adicionado futuramente como opção de configuração.
- **Sem telemetria** ou coleta de dados no MVP.

---

## 4. UI Design

### 4.1 Layout

```
┌─────────────────────────────────────────────────────┐
│ QuickNET                                   [_][□][×]│
├─────────────────────────────────────────────────────┤
│                                                     │
│  > 2 + 2                                            │
│  4                                                  │
│                                                     │
│  > File.ReadAllText(@"C:\test.txt")                 │
│  "Hello, World!"                                    │
│                                                     │
│  > var x = 10;                                      │
│  > var y = x * 3;                                   │
│  > y                                                │
│  30                                                 │
│                                                     │
│  (scrollable conversation area)                     │
│                                                     │
├─────────────────────────────────────────────────────┤
│ │ Enter to run  |  Shift+Enter for new line         │
├─────────────────────────────────────────────────────┤
│ Ready | C# | Timeout: 30s | Refs: 0 | Imports: 0    │
└─────────────────────────────────────────────────────┘
```

### 4.2 Interaction Flow

1. Usuário define a linguagem via `/lang cs` ou `/lang vb`.
2. Usuário digita código no campo de input.
3. Pressiona `Enter` (single-line) ou `Shift+Enter` (multi-line) para executar.
4. O input aparece no painel de conversação com prefixo `>`.
5. O resultado (ou erro) aparece logo abaixo.
6. O campo de input é limpo e o foco retorna a ele.
7. O histórico é salvo automaticamente em armazenamento local.
8. Configurações de sessão (linguagem, timeout, refs, imports) são salvas automaticamente.

### 4.3 Input Modes

| Modo | Gatilho | Comportamento |
|---|---|---|
| **Single-line** | `Enter` | O conteúdo da linha atual é submetido. |
| **Multi-line** | `Shift+Enter` | Todo o conteúdo do input (incluindo quebras de linha) é submetido como um bloco único. |

---

## 5. Core Engine Design

### 5.1 Language Support

| Linguagem | Namespace Roslyn | Extensão template | Using/Imports padrão |
|---|---|---|---|
| C# | `Microsoft.CodeAnalysis.CSharp` | `.cs` | `System`, `System.Collections.Generic`, `System.IO`, `System.Linq`, `System.Text`, `System.Threading.Tasks` |
| VB.NET | `Microsoft.CodeAnalysis.VisualBasic` | `.vb` | `System`, `System.Collections.Generic`, `System.IO`, `System.Linq`, `System.Text`, `System.Threading.Tasks` |

**v1.1:** Usings/Imports padrão são complementados dinamicamente com os namespaces adicionados via `/import` (ou `/using`), persistidos em `SessionState.ExtraImports`. Da mesma forma, as referências padrão de assembly são complementadas com as adicionadas via `/reference`, persistidas em `SessionState.ExtraReferences`.

### 5.2 Execution Pipeline (v1.1)

```
Input do Usuário
       │
       ├── Começa com "/"?
       │      │
       │      v
       │   ┌──────────────────┐
       │   │ MetaCmdParser    │  Detecta comando e argumentos
       │   └──────┬───────────┘
       │          v
       │   ┌──────────────────┐
       │   │ MetaCmdService   │  Executa comando contra SessionState
       │   └──────┬───────────┘
       │          v
       │   ┌──────────────────┐
       │   │ Display Result   │  Exibe no painel de conversação
       │   └──────────────────┘
       │
       └── Não começa com "/"
              │
              v
       ┌────────────────────┐
       │ 1. Compile         │  Roslyn compila com refs extras + imports extras
       │ (via Compilation   │  (vindos do SessionState)
       │  Service)          │
       └──────┬─────────────┘
              v
       ┌────────────────────┐    ┌──────────────────┐
       │ 2. Execute         │───>│ Sucesso: captura │
       │ (via Execution     │    │ resultado +      │
       │  Service +         │    │ ConsoleOutput    │
       │  CancellationToken)│    └──────────────────┘
       └──────┬─────────────┘    ┌──────────────────┐
              │                  │ Timeout: exibe   │
              └─────────────────>│ erro formatado   │
                                 └──────────────────┘
                                 ┌──────────────────┐
                                 │ Falha: exibe     │
                                 │ erro formatado   │
                                 └──────────────────┘
              v
       ┌──────────────────┐
       │  3. Display      │  Formata resultado (ToString / JSON / exception)
       └──────────────────┘
```

### 5.3 History Persistence

- Local de armazenamento: `%APPDATA%\QuickNET\history.json`
- Formato: JSON array com objetos `{ timestamp, language, input, output, isError }`
- Limite inicial do MVP: últimas 500 entradas
- Carregamento ao iniciar, salvamento a cada execução

### 5.4 Meta-command Engine (v1.1)

O QuickNET detecta inputs iniciados com `/` e os roteia para um motor de meta-comandos em vez de compilá-los. O parser segue o formato `/comando [argumentos]`.

**Pipeline de meta-comando:**

```
Input do Usuário
       │
       ├── Começa com "/"?
       │      │
       │      v
       │   MetaCommandParser.Parse(input)
       │      │
       │      v
       │   MetaCommandService.Execute(command, args, sessionState)
       │      │
       │      └──> Exibe resultado no painel (não compila)
       │
       └── Não começa com "/"
              │
              v
           Compilação + Execução normal (usa sessionState para refs/imports/timeout)
```

**Comandos implementados:**

| Comando | Alias | Descrição | Exemplo |
|---|---|---|---|
| `/clear` | — | Limpa o painel de conversação e o histórico persistido | `/clear` |
| `/help` | — | Lista todos os meta-comandos disponíveis com breve descrição | `/help` |
| `/reference` | — | Adiciona um assembly à lista de referências extras da sessão | `/reference System.Text.Json` |
| `/import` | `/using` | Adiciona um namespace aos imports globais da sessão | `/import System.Text.Json` |
| `/references` | — | Lista os assemblies atualmente referenciados (padrão + extras) | `/references` |
| `/imports` | — | Lista os namespaces atualmente importados (padrão + extras) | `/imports` |
| `/timeout` | — | Define o timeout de execução em segundos (0 = sem limite) | `/timeout 60` |
| `/lang` | — | Alterna a linguagem ativa: `cs` (C#) ou `vb` (VB.NET) | `/lang vb` |

**Tratamento de erros nos meta-comandos:**
- Comando desconhecido: exibe `Unknown command '/xyz'. Type /help for available commands.`
- Argumentos inválidos: exibe mensagem descritiva (ex.: `/timeout abc` → `Invalid timeout value 'abc'. Expected a number.`)
- `/reference` com assembly não encontrado: exibe `Assembly 'Foo.Bar' not found in the runtime.`

### 5.5 Session State & Persistence (v1.1)

A sessão mantém configurações que persistem entre execuções e entre reinicializações:

```csharp
public class SessionSettings
{
    public List<string> ExtraReferences { get; set; } = [];   // nomes de assemblies
    public List<string> ExtraImports { get; set; } = [];      // namespaces
    public int TimeoutSeconds { get; set; } = 30;              // 0 = no limit
    public string Language { get; set; } = "CSharp";           // "CSharp" ou "VisualBasic"
}
```

- **Persistência:** `%APPDATA%\QuickNET\settings.json` — salvo automaticamente a cada mutação.
- **Carregamento:** Ao iniciar, carrega de `settings.json`. Se arquivo não existir ou estiver corrompido, usa defaults.
- **Fallback:** Se `settings.json` estiver corrompido, inicia com defaults e sobrescreve o arquivo corrompido no primeiro save.
- **Serialização:** JSON com `WriteIndented = true` e `PropertyNamingPolicy = CamelCase`.

### 5.6 Dynamic Assembly References (v1.1)

O `/reference` permite adicionar assemblies à compilação dinamicamente. O serviço de resolução:

1. Recebe o nome parcial do assembly (ex.: `System.Text.Json`).
2. Tenta carregar via `Assembly.Load(assemblyName)` do runtime atual.
3. Se encontrado, extrai o `Assembly.Location` e adiciona como `MetadataReference` na compilação.
4. Se não encontrado, exibe erro.

As referências padrão (definidas no TASKS-2) permanecem sempre disponíveis. As extras são adicionadas cumulativamente.

Namespaces extras (`/import`) são injetados nos templates como `using`/`Imports` adicionais antes do código do usuário.

### 5.7 Execution Timeout (v1.1)

O timeout é implementado via `CancellationTokenSource`:

1. `ExecutionService.Execute()` recebe um `CancellationToken` opcional.
2. Se timeout > 0, cria `CancellationTokenSource` com `TimeSpan.FromSeconds(timeout)`.
3. Antes de `method.Invoke()`, verifica `token.IsCancellationRequested`.
4. O código do usuário não recebe o token diretamente — a interrupção é cooperativa via `Thread.Abort` não usado. Em vez disso, usa-se `Task.Run` com timeout:
   - O método `Execute()` é invocado dentro de uma `Task` com `.Wait(timeout)`.
   - Se o timeout expirar, a task é descartada e o `AssemblyLoadContext` é unloaded.
5. O resultado exibe `Execution timed out after {N} seconds.` como erro.

**Limitações conhecidas:**
- O cancelamento **não** interrompe código nativo ou chamadas bloqueantes de I/O dentro do snippet do usuário.
- Loops infinitos puros (`while(true){}`) **não** são interrompidos pelo timeout via `Task.Wait()` — o thread continua executando. Esta é uma limitação aceita para a v1.1.

---

## 6. Testing Strategy

### 6.1 Test Framework

- **Framework**: MSTest
- **Runner**: `dotnet test`
- **Padrão de projeto**: Um projeto de testes (`QuickNET.Tests`) separado do projeto principal.

### 6.2 Test Plan

| Camada | O que testar | Tipo |
|---|---|---|
| **Template Engine** | Geração correta de código para C# e VB.NET com diferentes inputs (expressões, blocos, statements). | Unit |
| **Compiler** | Compilação bem-sucedida de código válido; erros de compilação para código inválido; captura de diagnostics do Roslyn. | Unit |
| **Executor** | Execução de expressões com tipos variados (int, string, bool, object); propagação de exceções de runtime; captura de output (Console.WriteLine). | Unit |
| **History Manager** | Serialização/desserialização do histórico; limite de entradas; carregamento de arquivo corrompido (fallback). | Unit |
| **Language Switching** | Validação de que a troca de linguagem gera o template correto. | Unit |
| **ViewModels (Avalonia)** | Binding e comandos da UI via testes de ViewModel (sem necessidade de headless rendering). | Unit |
| **Integração** | Pipeline completo: input → compilação → execução → output para cenários representativos. | Integration |
| **Meta-command Parser** | Detecção de prefixo `/`, extração de comando e argumentos, comandos desconhecidos. | Unit |
| **Meta-command Service** | Execução de cada comando (`/clear`, `/help`, `/lang`, `/timeout`, `/reference`, `/import`, `/references`, `/imports`), tratamento de argumentos inválidos. | Unit |
| **Session State** | Persistência de `settings.json`, carregamento/fallback, mutações atômicas. | Unit |
| **Assembly Resolution** | Resolução de nomes parciais para assemblies do runtime, fallback para não encontrados. | Unit |
| **Timeout** | Execução com timeout via `Task.Wait`, timeout expirado retorna erro, timeout zero = sem limite. | Unit |
| **Dynamic Compilation** | Compilação com referências e imports extras injetados dinamicamente. | Integration |

### 6.3 Coverage Target

- **Mínimo**: 70% de cobertura nos módulos core (Engine, History, Templates).
- **Desejável**: 85%+ em branches críticos (caminho de compilação, caminho de erro).

---

## 7. Risks & Roadmap

### 7.1 Technical Risks

| Risco | Impacto | Mitigação |
|---|---|---|
| **Tempo de compilação elevado** para snippets complexos | Experiência degradada | Cache de assemblies do framework; medição de performance desde o MVP. |
| **Memory leak** por assemblies acumulados em memória | Crash após muitas execuções | Uso de `AssemblyLoadContext` isolado e descartável por execução (já especificado no compilation model). |
| **Ausência de syntax highlighting** reduz apelo visual | Adoção menor | Será introduzido na v1.2 como highlighting básico (palavras-chave + strings) com Avalonia.TextFormatting. |
| **Diferenças de comportamento C# vs VB.NET** no Roslyn | Funcionalidade inconsistente | Testar ambos os caminhos igualmente desde o início. |

### 7.2 Phased Roadmap

#### MVP (v1.0) — "QuickNET Core" ✅
- [x] Input single-line (Enter) e multi-line (Shift+Enter)
- [x] Compilação e execução C# e VB.NET via Roslyn
- [x] Painel de conversação scrollável
- [x] Seletor de linguagem (ComboBox)
- [x] Persistência de histórico
- [x] Distribuição Windows (MSI/EXE + ZIP)
- [x] Testes unitários com cobertura >= 70%

#### v1.1 — "Meta & Session" ✅
- [x] Meta-comandos: `/clear`, `/help`, `/reference`, `/import` (`/using`), `/references`, `/imports`, `/timeout`, `/lang`
- [x] Persistência de configurações de sessão (`settings.json`): referências, imports, timeout, linguagem
- [x] Dynamic assembly references via `/reference`
- [x] Dynamic namespace imports via `/import`
- [x] Timeout de execução configurável via `/timeout`
- [x] Barra de status com linguagem, timeout, refs e imports
- [x] Testes unitários para todas as novas features

#### v1.2 — "UX Avançada"
- [ ] Autocomplete / IntelliSense (popup)
- [ ] Syntax highlighting básico (palavras-chave, strings, comentários)
- [ ] Temas (claro/escuro)

#### v2.0 — "Cross-Platform & Extensibility"
- [ ] Suporte a Linux e macOS
- [ ] GitHub Actions CI/CD
- [ ] NuGet package import on-the-fly
- [ ] Suporte a carregamento de scripts (.csx/.vbx)

#### v3.0 — "CLI & Advanced"
- [ ] Versão CLI além da GUI
- [ ] Debugger integrado

---

## 8. Open Topics (TBD)

Os seguintes pontos ficam em aberto para decisão futura ou discussão adicional:

1. **Formato exato do histórico** — JSON lines vs JSON array; compressão para arquivos grandes.
2. **Distribuição**: WiX Toolset vs Velopack vs alternativa para o instalador MSI.
3. **NuGet packages on-the-fly**: como permitir que o usuário adicione referências a pacotes NuGet em tempo de execução (v2.0).
4. **Licenciamento**: open-source (MIT, Apache 2.0) vs source-available.
5. **Auto-update**: mecanismo de atualização automática integrado (Velopack suporta).

---

## Appendix A: References

- [Microsoft.CodeAnalysis (Roslyn)](https://github.com/dotnet/roslyn)
- [Avalonia UI](https://avaloniaui.net/)
- [MSTest](https://github.com/microsoft/testfx)
- [Velopack](https://github.com/velopack/velopack)
- [.NET 10 Preview](https://github.com/dotnet/core/tree/main/release-notes/10.0)
