using System.Text;
using QuickNET.Models;

namespace QuickNET.Templates;

public class CSharpTemplateEngine : ITemplateEngine
{
    public Language SupportedLanguage => Language.CSharp;

    public string GenerateCode(string userCode, IReadOnlyList<string>? extraImports = null)
    {
        var sb = new StringBuilder();

        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.IO;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Text;");
        sb.AppendLine("using System.Threading.Tasks;");

        if (extraImports != null)
        {
            foreach (var ns in extraImports.Distinct())
                sb.AppendLine($"using {ns};");
        }

        sb.AppendLine();
        sb.AppendLine("public static class QuickNETSession");
        sb.AppendLine("{");
        sb.AppendLine("    public static string __ConsoleOutput;");
        sb.AppendLine();
        sb.AppendLine("    public static object Execute()");
        sb.AppendLine("    {");
        sb.AppendLine("        var __sw = new StringWriter();");
        sb.AppendLine("        var __originalOut = Console.Out;");
        sb.AppendLine("        Console.SetOut(__sw);");
        sb.AppendLine("        try");
        sb.AppendLine("        {");

        var trimmed = userCode.TrimEnd('\r', '\n', ' ');

        if (IsExpression(trimmed))
        {
            sb.AppendLine($"            return {trimmed};");
        }
        else
        {
            foreach (var line in trimmed.Split('\n'))
            {
                sb.AppendLine($"            {line.TrimEnd('\r')}");
            }

            if (!trimmed.Contains("return ", StringComparison.Ordinal))
            {
                sb.AppendLine("            return null;");
            }
        }

        sb.AppendLine("        }");
        sb.AppendLine("        finally");
        sb.AppendLine("        {");
        sb.AppendLine("            Console.SetOut(__originalOut);");
        sb.AppendLine("            __ConsoleOutput = __sw.ToString();");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static bool IsExpression(string code)
    {
        if (code.Contains("return ", StringComparison.Ordinal))
            return false;

        if (code.Contains(';'))
            return false;

        var statementKeywords = new[]
        {
            "for ", "if ", "while ", "switch ", "using ", "namespace ", "class "
        };

        foreach (var kw in statementKeywords)
        {
            if (code.Contains(kw, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }
}
