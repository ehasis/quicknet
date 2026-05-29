using QuickNET.Compilation;
using QuickNET.Execution;
using QuickNET.Models;
using QuickNET.Session;

namespace QuickNET;

public class ReplEngine
{
    private readonly CompilationService _compilation;
    private readonly ExecutionService _execution;
    private readonly SessionState _sessionState;

    public ReplEngine(CompilationService compilation, ExecutionService execution,
        SessionState sessionState)
    {
        _compilation = compilation;
        _execution = execution;
        _sessionState = sessionState;
    }

    public ExecutionResult Execute(string sourceCode, Language language)
    {
        var compilationInput = new CompilationInput(
            sourceCode,
            language,
            _sessionState.ExtraReferences,
            _sessionState.ExtraImports
        );

        var compilationResult = _compilation.Compile(compilationInput);

        if (!compilationResult.Success)
        {
            var errors = string.Join("\n",
                compilationResult.Diagnostics
                    .Where(d => d.Severity == "Error")
                    .Select(d => $"{d.Severity}: {d.Message} (Line {d.Line}, Col {d.Column})"));
            return new ExecutionResult(false, null, errors, null);
        }

        var executionInput = new ExecutionInput(compilationResult.AssemblyBytes!);
        return _execution.Execute(executionInput);
    }
}
