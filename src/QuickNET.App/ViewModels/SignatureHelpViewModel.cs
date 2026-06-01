using System.Text;
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
    private int _activeParameterStart = -1;

    [ObservableProperty]
    private int _activeParameterLength;

    public void Show(IReadOnlyList<SignatureHelpSegment> segments)
    {
        if (segments.Count == 0)
        {
            Hide();
            return;
        }

        var sb = new StringBuilder();
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
