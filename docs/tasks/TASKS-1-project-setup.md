# TASKS-1: Project Setup & Solution Structure

**Block:** 1 de 7
**Depends on:** Nenhum (primeiro bloco)
**PRD Reference:** `docs/PRD.md` — Seções 3.2, 3.4

---

## Objective

Criar a estrutura de solution e projetos .NET 10 para o QuickNET, com todos os pacotes NuGet necessários e configuração base.

---

## Tasks

### 1.1 Criar a solution

Criar um arquivo `.slnx` (novo formato do .NET 10) na raiz do repositório:

```
quicknet.slnx
```

A solution deve conter os seguintes projetos:

| Projeto | Tipo | Descrição |
|---|---|---|
| `src/QuickNET.Core` | Class Library (net10.0) | Engine de compilação, execução e histórico |
| `src/QuickNET.App` | Avalonia Application (net10.0-windows) | Aplicação desktop |
| `tests/QuickNET.Tests` | MSTest Project (net10.0-windows) | Testes unitários |

Comandos esperados (na ordem):
```pwsh
dotnet new slnx --name quicknet
dotnet new classlib -n QuickNET.Core -o src/QuickNET.Core --framework net10.0
dotnet new avalonia.app -n QuickNET.App -o src/QuickNET.App --framework net10.0
dotnet new mstest -n QuickNET.Tests -o tests/QuickNET.Tests --framework net10.0
dotnet slnx quicknet.slnx add src/QuickNET.Core src/QuickNET.App tests/QuickNET.Tests
```

### 1.2 Atualizar Directory.Build.props

O arquivo `Directory.Build.props` já existe na raiz. Adicionar as propriedades:

```xml
<Project>
  <PropertyGroup>
    <UseArtifactsOutput>true</UseArtifactsOutput>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AnalysisLevel>latest</AnalysisLevel>
    <RootNamespace>QuickNET</RootNamespace>
  </PropertyGroup>
</Project>
```

**Nota:** `QuickNET.App` e `QuickNET.Tests` precisam de `<TargetFramework>net10.0-windows</TargetFramework>` — definir no `.csproj` individual (sobrescreve o do props).

### 1.3 Adicionar referências de projeto

- `QuickNET.App` → referencia `QuickNET.Core`
- `QuickNET.Tests` → referencia `QuickNET.Core`
- `QuickNET.Tests` → referencia `QuickNET.App` (para testes de ViewModel)

Fazer via `dotnet add reference`.

### 1.4 Instalar pacotes NuGet — QuickNET.Core

```pwsh
dotnet add src/QuickNET.Core package Microsoft.CodeAnalysis.CSharp
dotnet add src/QuickNET.Core package Microsoft.CodeAnalysis.VisualBasic
```

### 1.5 Instalar pacotes NuGet — QuickNET.App

```pwsh
dotnet add src/QuickNET.App package Avalonia.Desktop
dotnet add src/QuickNET.App package Avalonia.Themes.Fluent
dotnet add src/QuickNET.App package Avalonia.Diagnostics
```

A versão do Avalonia deve ser estável compatível com .NET 10. No momento da execução, usar a mais recente disponível:

```pwsh
# Para descobrir a versão mais recente:
dotnet package search Avalonia.Desktop --take 1
```

### 1.6 Ajustar TargetFramework dos projetos de UI e testes

Editar `src/QuickNET.App/QuickNET.App.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <OutputType>WinExe</OutputType>
    <BuiltInComInteropSupport>true</BuiltInComInteropSupport>
  </PropertyGroup>
  <!-- ... -->
</Project>
```

Editar `tests/QuickNET.Tests/QuickNET.Tests.csproj`:

```xml
<Project Sdk="MSTest.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
  </PropertyGroup>
  <!-- ... -->
</Project>
```

### 1.7 Remover arquivos boilerplate desnecessários

Apagar `Class1.cs` de `QuickNET.Core` e `QuickNET.Tests` se existirem.

### 1.8 Criar diretórios vazios para namespaces futuros

```
src/QuickNET.Core/
  Models/        (.gitkeep)
  Compilation/   (.gitkeep)
  Execution/     (.gitkeep)
  History/       (.gitkeep)
  Templates/     (.gitkeep)

src/QuickNET.App/
  Models/        (.gitkeep)
  ViewModels/    (.gitkeep)
  Views/         (.gitkeep)

tests/QuickNET.Tests/
  Compilation/   (.gitkeep)
  Execution/     (.gitkeep)
  History/       (.gitkeep)
  ViewModels/    (.gitkeep)
```

---

## Acceptance Criteria

- [ ] `dotnet build` compila toda a solution sem erros.
- [ ] `dotnet test` executa (ainda sem testes, mas o runner funciona).
- [ ] `Directory.Build.props` contém `Nullable`, `ImplicitUsings` e `AnalysisLevel`.
- [ ] `QuickNET.Core` referencia `Microsoft.CodeAnalysis.CSharp` e `Microsoft.CodeAnalysis.VisualBasic`.
- [ ] `QuickNET.App` referencia `Avalonia.Desktop`, `Avalonia.Themes.Fluent`.
- [ ] `QuickNET.App` referencia `QuickNET.Core`.
- [ ] `QuickNET.Tests` referencia `QuickNET.Core` e `QuickNET.App`.

---

## Notes for AI Agent

- O `dotnet new slnx` é o novo formato de solution do .NET 9+. Se falhar, usar `dotnet new sln` (formato `.sln` tradicional).
- O `MSTest.Sdk` é o novo estilo (não usar `Microsoft.NET.Test.Sdk` + `MSTest.TestAdapter` + `MSTest.TestFramework` separados).
- Para `Avalonia.App`, o template pode pedir confirmação interativa. Usar flags `--no-restore` se necessário e depois restaurar.
- Os `.gitkeep` são opcionais — podem ser criados como arquivos vazios com `New-Item`.
- Verificar com `dotnet --version` se o SDK é >= 10.0.100 antes de começar.
