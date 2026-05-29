using QuickNET.Session;

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

    public static AppTheme ParseTheme(string? theme)
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
        try
        {
            var key = Microsoft.Win32.Registry.CurrentUser
                .OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var appsUseLightTheme = key?.GetValue("AppsUseLightTheme");
            return appsUseLightTheme is 0 ? AppTheme.Dark : AppTheme.Light;
        }
        catch
        {
            return AppTheme.Light;
        }
    }
}
