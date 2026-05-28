using System.Text;
using QuickNET.Models;

namespace QuickNET.Templates;

public class VbTemplateEngine : ITemplateEngine
{
    public Language SupportedLanguage => Language.VisualBasic;

    private const string Header = """
        Imports System
        Imports System.Collections.Generic
        Imports System.IO
        Imports System.Linq
        Imports System.Text
        Imports System.Threading.Tasks

        Public Module QuickNETSession
            Public Function Execute() As Object
        """;

    private const string Footer = """
            End Function
        End Module
        """;

    public string GenerateCode(string userCode)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);

        var trimmed = userCode.TrimEnd('\r', '\n', ' ');

        if (IsExpression(trimmed))
        {
            sb.AppendLine($"        Return {trimmed}");
        }
        else
        {
            foreach (var line in trimmed.Split('\n'))
            {
                sb.AppendLine($"        {line.TrimEnd('\r')}");
            }
        }

        sb.Append(Footer);
        return sb.ToString();
    }

    private static bool IsExpression(string code)
    {
        var statementKeywords = new[]
        {
            "Dim ", "If ", "For ", "While ", "Select ", "Using ",
            "Namespace ", "Class ", "Module ", "Sub ", "Function ",
            "End ", "Return "
        };

        foreach (var kw in statementKeywords)
        {
            if (code.Contains(kw, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}
