using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using QuickNET.App.ViewModels;
using QuickNET.App.Views;

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
                mainWindow.DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>();
            }
            desktop.MainWindow = mainWindow;
        }
        base.OnFrameworkInitializationCompleted();
    }
}
