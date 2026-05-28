# TASKS-5: UI Shell — Avalonia Window & Layout

**Block:** 5 de 7
**Depends on:** TASKS-1 (project setup)
**PRD Reference:** `docs/PRD.md` — Seções 4.1, 4.2, 4.3

---

## Objective

Construir a janela principal do QuickNET com o layout de conversação (painel de input/output + ComboBox de linguagem + barra de status), usando Avalonia com tema Fluent. Neste bloco, usar ViewModels **stub** — a lógica real será implementada no TASKS-6.

---

## Layout Specification

```
┌─────────────────────────────────────────────────────┐
│ QuickNET                                   [_][□][×]│
├─────────────────────────────────────────────────────┤
│ Lang: [C# ▼]                              [Clear]   │
├─────────────────────────────────────────────────────┤
│                                                      │
│  > 2 + 2                                             │
│  4                                                   │
│                                                      │
│  > File.ReadAllText(@"C:\test.txt")                  │
│  "Hello, World!"                                     │
│                                                      │
│  (scrollable conversation area — ItemsControl)       │
│                                                      │
├─────────────────────────────────────────────────────┤
│ │ Shift+Enter to run   │  C#   │  ............... │  │
├─────────────────────────────────────────────────────┤
│ Status bar                                            │
└─────────────────────────────────────────────────────┘
```

---

## Tasks

### 5.1 Criar App.axaml

Substituir o conteúdo de `src/QuickNET.App/App.axaml`:

```xml
<Application xmlns="https://github.com/avaloniaui"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             x:Class="QuickNET.App.App">
    <Application.DataTemplates>
        <!-- Data templates serão adicionados no TASKS-6 -->
    </Application.DataTemplates>
    <Application.Styles>
        <FluentTheme />
    </Application.Styles>
</Application>
```

### 5.2 Criar App.axaml.cs

Arquivo code-behind `src/QuickNET.App/App.axaml.cs`:

```csharp
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using QuickNET.App.Views;

namespace QuickNET.App;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }
        base.OnFrameworkInitializationCompleted();
    }
}
```

### 5.3 Criar MainWindow.axaml

Arquivo em `src/QuickNET.App/Views/MainWindow.axaml`:

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        x:Class="QuickNET.App.Views.MainWindow"
        Title="QuickNET"
        Width="900" Height="620"
        MinWidth="600" MinHeight="400"
        WindowStartupLocation="CenterScreen">

    <Design.DataContext>
        <!-- Stub ViewModel; será substituído no TASKS-6 quando tivermos DI -->
    </Design.DataContext>

    <DockPanel>
        <!-- Toolbar -->
        <StackPanel DockPanel.Dock="Top" Orientation="Horizontal"
                    Margin="8,6" Spacing="10">
            <TextBlock Text="Lang:" VerticalAlignment="Center" />
            <ComboBox x:Name="LanguageSelector" Width="120"
                      SelectedIndex="0">
                <ComboBoxItem>C#</ComboBoxItem>
                <ComboBoxItem>VB.NET</ComboBoxItem>
            </ComboBox>
            <Button x:Name="ClearButton" Content="Clear"
                    HorizontalAlignment="Right" />
        </StackPanel>

        <!-- Conversation Panel -->
        <ScrollViewer x:Name="ConversationScroller"
                      AllowAutoHide="False">
            <ItemsControl x:Name="ConversationPanel"
                          Margin="8">
                <ItemsControl.ItemTemplate>
                    <DataTemplate>
                        <!-- Template será refinado no TASKS-6 -->
                        <StackPanel Margin="0,2">
                            <TextBlock Text="{Binding DisplayText}"
                                       FontFamily="Cascadia Code, Consolas, monospace"
                                       FontSize="13" />
                        </StackPanel>
                    </DataTemplate>
                </ItemsControl.ItemTemplate>
            </ItemsControl>
        </ScrollViewer>

        <!-- Input Area -->
        <Border DockPanel.Dock="Bottom"
                BorderBrush="{DynamicResource SystemControlForegroundBaseMediumBrush}"
                BorderThickness="0,1,0,0"
                Padding="8,4">
            <DockPanel>
                <StackPanel DockPanel.Dock="Bottom" Orientation="Horizontal"
                            Margin="0,4,0,0">
                    <TextBlock Text="Shift+Enter to run"
                               Foreground="{DynamicResource SystemControlForegroundBaseMediumBrush}"
                               FontSize="11"
                               VerticalAlignment="Center" />
                </StackPanel>
                <TextBox x:Name="InputBox"
                         AcceptsReturn="True"
                         TextWrapping="Wrap"
                         FontFamily="Cascadia Code, Consolas, monospace"
                         FontSize="14"
                         MinHeight="28"
                         MaxHeight="200"
                         Watermark="Type your code here..." />
            </DockPanel>
        </Border>

        <!-- Status Bar -->
        <Border DockPanel.Dock="Bottom"
                Background="{DynamicResource SystemControlBackgroundBaseLowBrush}"
                Padding="8,2">
            <TextBlock x:Name="StatusText"
                       Text="Ready"
                       FontSize="11" />
        </Border>
    </DockPanel>
</Window>
```

### 5.4 Criar MainWindow.axaml.cs (code-behind básico)

Arquivo `src/QuickNET.App/Views/MainWindow.axaml.cs`:

Deve ser um stub que compila. A lógica de interação será adicionada no TASKS-6.

```csharp
using Avalonia.Controls;

namespace QuickNET.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
```

### 5.5 Criar Program.cs (entry point)

Arquivo em `src/QuickNET.App/Program.cs`:

```csharp
using Avalonia;
using System;

namespace QuickNET.App;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
```

### 5.6 Remover MainView.axaml boilerplate

O template Avalonia cria `MainView.axaml` e `MainWindow.axaml` por padrão. Se houver `MainView.axaml`, apagá-lo (usaremos `MainWindow` como janela raiz).

---

## Acceptance Criteria

- [ ] `dotnet run --project src/QuickNET.App` abre uma janela com o título "QuickNET".
- [ ] A janela exibe: toolbar com ComboBox (C#/VB.NET) + botão Clear, painel de conversação scrollável, campo de input multi-linha, e barra de status "Ready".
- [ ] O ComboBox tem os itens "C#" e "VB.NET" selecionáveis.
- [ ] O campo de input aceita múltiplas linhas (`AcceptsReturn="True"`).
- [ ] A janela tem tamanho mínimo de 600x400 e inicial de 900x620.
- [ ] A fonte do input e da conversação é monoespaçada (Cascadia Code com fallback Consolas).
- [ ] Nenhuma ação ocorre ao pressionar Enter ou Shift+Enter ainda (será implementado no TASKS-6).

---

## Notes for AI Agent

- O template Avalonia pode ter gerado vários arquivos boilerplate. Manter apenas: `App.axaml`, `App.axaml.cs`, `MainWindow.axaml`, `MainWindow.axaml.cs`, `Program.cs`.
- O `DockPanel` não é built-in no Avalonia — precisa do pacote `Avalonia.Controls` (já incluso via metapackage). Se não funcionar, usar `Grid` com linhas `Auto, *, Auto, Auto`.
- Como alternativa mais segura ao DockPanel, usar este layout com Grid:

```xml
<Grid RowDefinitions="Auto,*,Auto,Auto">
    <!-- Toolbar: Grid.Row="0" -->
    <!-- Conversation: Grid.Row="1" -->
    <!-- Input: Grid.Row="2" -->
    <!-- Status: Grid.Row="3" -->
</Grid>
```

- O `Watermark` no TextBox é uma propriedade do Avalonia (`Watermark="..."`).
- Para o ícone `[_][□][×]` da janela, não é necessário customizar — o Avalonia gerencia a chrome nativa.
- `WindowStartupLocation="CenterScreen"` centraliza a janela ao abrir.
