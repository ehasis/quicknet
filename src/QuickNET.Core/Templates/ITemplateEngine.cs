using QuickNET.Models;

namespace QuickNET.Templates;

public interface ITemplateEngine
{
    string GenerateCode(string userCode, IReadOnlyList<string>? extraImports = null);
    Language SupportedLanguage { get; }
}
