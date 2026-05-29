namespace QuickNET.Models;

public class SessionSettings
{
    public List<string> ExtraReferences { get; set; } = [];
    public List<string> ExtraImports { get; set; } = [];
    public int TimeoutSeconds { get; set; } = 30;
    public string Language { get; set; } = "CSharp";
    public string Theme { get; set; } = "System";
}
