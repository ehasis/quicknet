using QuickNET.Models;

namespace QuickNET.Templates;

public interface ITemplateEngine
{
    string GenerateCode(string userCode);
    Language SupportedLanguage { get; }
}
