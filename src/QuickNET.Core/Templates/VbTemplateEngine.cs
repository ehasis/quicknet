using System.Text;
using QuickNET.Models;

namespace QuickNET.Templates;

public class VbTemplateEngine : ITemplateEngine
{
    public Language SupportedLanguage => Language.VisualBasic;

    public string GenerateCode(string userCode, IReadOnlyList<string>? extraImports = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Imports System");
        sb.AppendLine("Imports System.Collections.Generic");
        sb.AppendLine("Imports System.IO");
        sb.AppendLine("Imports System.Linq");
        sb.AppendLine("Imports System.Text");
        sb.AppendLine("Imports System.Threading.Tasks");

        if (extraImports != null)
        {
            foreach (var ns in extraImports.Distinct())
                sb.AppendLine($"Imports {ns}");
        }

        sb.AppendLine();
        sb.AppendLine("Public Module QuickNETSession");
        sb.AppendLine("    Public __ConsoleOutput As String");
        sb.AppendLine();
        sb.AppendLine("    Public Function Execute() As Object");
        sb.AppendLine("        Dim __sw As New StringWriter()");
        sb.AppendLine("        Dim __originalOut = Console.Out");
        sb.AppendLine("        Console.SetOut(__sw)");
        sb.AppendLine("        Try");

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

        sb.AppendLine("        Finally");
        sb.AppendLine("            Console.SetOut(__originalOut)");
        sb.AppendLine("            __ConsoleOutput = __sw.ToString()");
        sb.AppendLine("        End Try");
        sb.AppendLine("    End Function");
        sb.AppendLine("End Module");

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
