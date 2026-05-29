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
            Public __ConsoleOutput As String

            Public Function Execute() As Object
                Dim __sw As New StringWriter()
                Dim __originalOut = Console.Out
                Console.SetOut(__sw)
                Try
        """;

    private const string Footer = """
                Finally
                    Console.SetOut(__originalOut)
                    __ConsoleOutput = __sw.ToString()
                End Try
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
            sb.AppendLine($"            Return {trimmed}");
        }
        else
        {
            foreach (var line in trimmed.Split('\n'))
            {
                sb.AppendLine($"            {line.TrimEnd('\r')}");
            }

            if (!trimmed.Contains("Return ", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("            Return Nothing");
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
