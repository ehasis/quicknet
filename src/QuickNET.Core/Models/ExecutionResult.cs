namespace QuickNET.Models;

public record ExecutionResult(
    bool Success,
    string? Output,
    string? Error,
    string? ConsoleOutput
);
