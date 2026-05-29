namespace QuickNET.Models;

public record CompilationInput(
    string SourceCode,
    Language Language,
    IReadOnlyList<string>? ExtraReferences = null,
    IReadOnlyList<string>? ExtraImports = null
);
