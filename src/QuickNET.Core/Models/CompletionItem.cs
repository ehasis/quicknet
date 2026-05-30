namespace QuickNET.Models;

public class CompletionItem
{
    public string DisplayText { get; init; } = "";
    public string InsertText { get; init; } = "";
    public string? Description { get; init; }
    public CompletionItemKind Kind { get; init; }
}

public enum CompletionItemKind
{
    Unknown,
    Keyword,
    Method,
    Property,
    Field,
    Class,
    Struct,
    Interface,
    Enum,
    Namespace,
    Variable,
    Snippet
}
