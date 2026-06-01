using Avalonia;
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
        inlines?.Clear();

        if (string.IsNullOrEmpty(text) || inlines is null)
            return;

        IBrush defaultBrush = Brushes.White;
        IBrush accentBrush = Brushes.DodgerBlue;

        if (Application.Current is { } app)
        {
            if (app.TryFindResource("SystemControlForegroundBaseHighBrush", app.ActualThemeVariant, out var def))
                defaultBrush = def as IBrush ?? Brushes.White;
            if (app.TryFindResource("SystemControlForegroundAccentBrush", app.ActualThemeVariant, out var acc))
                accentBrush = acc as IBrush ?? Brushes.DodgerBlue;
        }

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
