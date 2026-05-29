using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using QuickNET.Models;
using QuickNET.Templates;

namespace QuickNET.Compilation;

public class CompilationService
{
    private readonly Dictionary<Language, ITemplateEngine> _engines;
    private readonly AssemblyResolutionService _assemblyResolver;

    private static readonly List<PortableExecutableReference> DefaultReferences = new()
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.IO.File).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.Text.Encoding).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Microsoft.VisualBasic.Constants).Assembly.Location),
        MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
    };

    public CompilationService(IEnumerable<ITemplateEngine> engines,
        AssemblyResolutionService assemblyResolver)
    {
        _engines = engines.ToDictionary(e => e.SupportedLanguage);
        _assemblyResolver = assemblyResolver;
    }

    public CompilationResult Compile(CompilationInput input)
    {
        if (!_engines.TryGetValue(input.Language, out var engine))
            return new CompilationResult
            {
                Success = false,
                Diagnostics =
                {
                    new DiagnosticMessage
                    {
                        Severity = "Error",
                        Message = $"No template engine found for language {input.Language}"
                    }
                }
            };

        var fullCode = engine.GenerateCode(input.SourceCode, input.ExtraImports);
        var sourceText = SourceText.From(fullCode);

        var references = new List<PortableExecutableReference>(DefaultReferences);
        if (input.ExtraReferences != null && input.ExtraReferences.Count > 0)
        {
            foreach (var refName in input.ExtraReferences)
            {
                var resolved = _assemblyResolver.Resolve(refName);
                if (resolved != null)
                    references.Add((PortableExecutableReference)resolved);
            }
        }

        var extraImportsCount = input.ExtraImports?.Count ?? 0;

        if (input.Language == Language.VisualBasic)
            return CompileVisualBasic(sourceText, references, extraImportsCount);

        return CompileCSharp(sourceText, references, extraImportsCount);
    }

    private static CompilationResult CompileCSharp(SourceText sourceText,
        List<PortableExecutableReference> references, int extraImportsCount)
    {
        var lineOffset = 18 + extraImportsCount;

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceText);
        var compilation = CSharpCompilation.Create(
            "QuickNETSession",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var emitResult = compilation.Emit(ms);

        var diagnostics = MapDiagnostics(emitResult.Diagnostics, lineOffset);

        if (!emitResult.Success)
            return new CompilationResult { Success = false, Diagnostics = diagnostics };

        return new CompilationResult
        {
            Success = true,
            AssemblyBytes = ms.ToArray(),
            Diagnostics = diagnostics
        };
    }

    private static CompilationResult CompileVisualBasic(SourceText sourceText,
        List<PortableExecutableReference> references, int extraImportsCount)
    {
        var lineOffset = 15 + extraImportsCount;

        var syntaxTree = VisualBasicSyntaxTree.ParseText(sourceText);
        var compilation = VisualBasicCompilation.Create(
            "QuickNETSession",
            new[] { syntaxTree },
            references,
            new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var emitResult = compilation.Emit(ms);

        var diagnostics = MapDiagnostics(emitResult.Diagnostics, lineOffset);

        if (!emitResult.Success)
            return new CompilationResult { Success = false, Diagnostics = diagnostics };

        return new CompilationResult
        {
            Success = true,
            AssemblyBytes = ms.ToArray(),
            Diagnostics = diagnostics
        };
    }

    private static List<DiagnosticMessage> MapDiagnostics(
        IEnumerable<Diagnostic> diagnostics, int lineOffset)
    {
        var result = new List<DiagnosticMessage>();

        foreach (var d in diagnostics)
        {
            var lineSpan = d.Location.GetLineSpan();
            var originalLine = lineSpan.StartLinePosition.Line - lineOffset;
            var originalColumn = lineSpan.StartLinePosition.Character;

            if (originalLine < 0)
                originalLine = 0;

            result.Add(new DiagnosticMessage
            {
                Severity = d.Severity.ToString(),
                Message = d.GetMessage(),
                Line = originalLine,
                Column = originalColumn
            });
        }

        return result;
    }
}
