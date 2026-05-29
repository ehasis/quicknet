using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using QuickNET.App.ViewModels;
using QuickNET.App.Views;
using System;

namespace QuickNET.App;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var services = new ServiceCollection();
        services.AddQuickNETCore();
        services.AddSingleton<MainWindowViewModel>();

        var provider = services.BuildServiceProvider();

        BuildAvaloniaApp(provider).StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp(IServiceProvider? serviceProvider = null)
        => AppBuilder.Configure(() => new App(serviceProvider))
            .UsePlatformDetect()
            .LogToTrace();
}
