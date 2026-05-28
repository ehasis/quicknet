using System.Text;
using QuickNET.Models;

namespace QuickNET.Templates;

public class CSharpTemplateEngine : ITemplateEngine
{
    public Language SupportedLanguage => Language.CSharp;

    private const string Header = """
        using System;
        using System.Collections.Generic;
        using System.IO;
        using System.Linq;
        using System.Text;
        using System.Threading.Tasks;

        public static class QuickNETSession
        {
            public static object Execute()
            {
        """;

    private const string Footer = """
            }
        }
        """;

    public string GenerateCode(string userCode)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Header);

        var trimmed = userCode.TrimEnd('\r', '\n', ' ');

        if (IsExpression(trimmed))
        {
            sb.AppendLine($"        return {trimmed};");
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
