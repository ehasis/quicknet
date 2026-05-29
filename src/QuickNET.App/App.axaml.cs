using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using QuickNET.App.ViewModels;
using QuickNET.App.Views;
using QuickNET.Theme;

namespace QuickNET.App;

public class App : Application
{
    private readonly IServiceProvider? _serviceProvider;

    public App() { }

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
            var mainWindow = new MainWindow();
            if (_serviceProvider != null)
            {
                var themeService = _serviceProvider.GetRequiredService<ThemeService>();
                mainWindow.DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>();

                ApplyTheme(themeService.CurrentTheme);

                themeService.ThemeChanged += (_, theme) => ApplyTheme(theme);
            }
            desktop.MainWindow = mainWindow;
        }
        base.OnFrameworkInitializationCompleted();
    }

    private static void ApplyTheme(AppTheme theme)
    {
        if (Current is not { } app) return;

        app.RequestedThemeVariant = theme switch
        {
            AppTheme.Dark => ThemeVariant.Dark,
            AppTheme.Light => ThemeVariant.Light,
            _ => ThemeVariant.Default
        };
    }
}
