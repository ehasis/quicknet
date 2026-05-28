namespace QuickNET.Models;

public class CompilationResult
{
    public bool Success { get; init; }
    public byte[]? AssemblyBytes { get; init; }
    public List<DiagnosticMessage> Diagnostics { get; init; } = [];
}

public class DiagnosticMessage
{
    public string Severity { get; init; } = "";
    public string Message { get; init; } = "";
    public int? Line { get; init; }
    public int? Column { get; init; }
}
