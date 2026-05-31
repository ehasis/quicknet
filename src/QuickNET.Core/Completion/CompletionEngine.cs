using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using QuickNET.Compilation;
using QuickNET.Models;
using RoslynCompletionItem = Microsoft.CodeAnalysis.Completion.CompletionItem;
using RoslynCompletionService = Microsoft.CodeAnalysis.Completion.CompletionService;

namespace QuickNET.Completion;

public class CompletionEngine
{
    private readonly AssemblyResolutionService _assemblyResolver;
    private AdhocWorkspace? _workspace;
    private Language _workspaceLanguage;
    private IReadOnlyList<string>? _workspaceExtraReferences;
    private IReadOnlyList<string>? _workspaceExtraImports;

    public CompletionEngine(AssemblyResolutionService assemblyResolver)
    {
        _assemblyResolver = assemblyResolver;
    }

    public async Task<IReadOnlyList<CompletionItem>> GetCompletionsAsync(
        string sourceCode,
        int cursorPosition,
        Language language,
        IReadOnlyList<string>? extraReferences = null,
        IReadOnlyList<string>? extraImports = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var (wrappedCode, adjustedPosition) = WrapForCompletion(sourceCode, cursorPosition, language);

        var workspace = GetOrCreateWorkspace(wrappedCode, language, extraReferences, extraImports);
        var project = workspace.CurrentSolution.Projects.FirstOrDefault();
        if (project is null)
            return Array.Empty<CompletionItem>();

        var document = project.Documents.FirstOrDefault();
        if (document is null)
            return Array.Empty<CompletionItem>();

        var sourceText = SourceText.From(wrappedCode);
        document = document.WithText(sourceText);

        var completionService = RoslynCompletionService.GetService(document);
        if (completionService is null)
            return Array.Empty<CompletionItem>();

        ct.ThrowIfCancellationRequested();

        var completions = await completionService.GetCompletionsAsync(document, adjustedPosition, cancellationToken: ct);
        if (completions is null)
            return Array.Empty<CompletionItem>();

        var filterText = "";
        if (completions.Span.Length > 0 && completions.Span.End <= sourceText.Length)
        {
            filterText = sourceText.GetSubText(completions.Span).ToString();
        }

        var defaultImports = GetAllImports(language, null);

        return completions.ItemsList
            .Where(i => !defaultImports.Contains(i.DisplayTextPrefix))
            .Where(i => string.IsNullOrEmpty(filterText)
                || (i.FilterText ?? i.DisplayText).StartsWith(filterText, StringComparison.OrdinalIgnoreCase))
            .Select(i => MapToCompletionItem(i, document))
            .ToList();
    }

    private static (string wrappedCode, int adjustedPosition) WrapForCompletion(
        string sourceCode, int cursorPosition, Language language)
    {
        if (language == Language.CSharp)
        {
            const string prefix = "class __W { void __M() { ";
            const string suffix = " } }";
            return (prefix + sourceCode + suffix, cursorPosition + prefix.Length);
        }

        const string vbPrefix = "Module __W\n    Sub __M()\n        Dim __x = ";
        const string vbSuffix = "\n    End Sub\nEnd Module";
        return (vbPrefix + sourceCode + vbSuffix, cursorPosition + vbPrefix.Length);
    }

    private AdhocWorkspace GetOrCreateWorkspace(
        string sourceCode, Language language,
        IReadOnlyList<string>? extraReferences, IReadOnlyList<string>? extraImports)
    {
        bool needsRecreate = _workspace is null
            || _workspaceLanguage != language
            || !ListsEqual(_workspaceExtraReferences, extraReferences)
            || !ListsEqual(_workspaceExtraImports, extraImports);

        if (needsRecreate)
        {
            _workspace = CreateWorkspace(sourceCode, language, extraReferences, extraImports);
            _workspaceLanguage = language;
            _workspaceExtraReferences = extraReferences?.ToList();
            _workspaceExtraImports = extraImports?.ToList();
        }

        return _workspace;
    }

    private AdhocWorkspace CreateWorkspace(
        string sourceCode, Language language,
        IReadOnlyList<string>? extraReferences, IReadOnlyList<string>? extraImports)
    {
        var workspace = new AdhocWorkspace();
        var projectInfo = ProjectInfo.Create(
            ProjectId.CreateNewId(),
            VersionStamp.Create(),
            language == Language.CSharp ? "QuickNETCompletion-CSharp" : "QuickNETCompletion-VB",
            "QuickNETCompletion",
            language == Language.CSharp ? LanguageNames.CSharp : LanguageNames.VisualBasic);

        var project = workspace.AddProject(projectInfo);

        var allRefs = GetAllReferences(extraReferences);
        foreach (var mref in allRefs)
        {
            project = project.AddMetadataReference(mref);
        }

        var allImports = GetAllImports(language, extraImports);
        if (language == Language.CSharp)
        {
            project = project.WithCompilationOptions(
                ((CSharpCompilationOptions)project.CompilationOptions!)
                    .WithUsings(allImports));
        }
        else
        {
            project = project.WithCompilationOptions(
                ((VisualBasicCompilationOptions)project.CompilationOptions!)
                    .WithGlobalImports(allImports.Select(i => GlobalImport.Parse(i))));
        }

        var ext = language == Language.CSharp ? ".cs" : ".vb";
        var document = project.AddDocument("code" + ext, SourceText.From(sourceCode));
        workspace.TryApplyChanges(document.Project.Solution);

        return workspace;
    }

    private List<MetadataReference> GetAllReferences(IReadOnlyList<string>? extraReferences)
    {
        var refs = new List<MetadataReference>();

        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location)))
        {
            try { refs.Add(MetadataReference.CreateFromFile(asm.Location)); }
            catch { }
        }

        if (extraReferences is not null)
        {
            foreach (var name in extraReferences)
            {
                var resolved = _assemblyResolver.Resolve(name);
                if (resolved is not null)
                    refs.Add(resolved);
            }
        }

        return refs;
    }

    private static List<string> GetAllImports(Language language, IReadOnlyList<string>? extraImports)
    {
        var imports = new List<string>
        {
            "System", "System.Collections.Generic", "System.IO",
            "System.Linq", "System.Text", "System.Threading.Tasks"
        };

        if (extraImports is not null)
            imports.AddRange(extraImports.Except(imports));

        return imports;
    }

    private static CompletionItem MapToCompletionItem(
        RoslynCompletionItem roslynItem, Document document)
    {
        return new CompletionItem
        {
            DisplayText = roslynItem.DisplayText,
            InsertText = roslynItem.DisplayText,
            Description = roslynItem.InlineDescription,
            Kind = MapKind(roslynItem.Tags)
        };
    }

    private static CompletionItemKind MapKind(ImmutableArray<string> tags)
    {
        if (tags.Contains("Keyword")) return CompletionItemKind.Keyword;
        if (tags.Contains("Method")) return CompletionItemKind.Method;
        if (tags.Contains("Property")) return CompletionItemKind.Property;
        if (tags.Contains("Field")) return CompletionItemKind.Field;
        if (tags.Contains("Class")) return CompletionItemKind.Class;
        if (tags.Contains("Struct")) return CompletionItemKind.Struct;
        if (tags.Contains("Interface")) return CompletionItemKind.Interface;
        if (tags.Contains("Enum")) return CompletionItemKind.Enum;
        if (tags.Contains("Namespace")) return CompletionItemKind.Namespace;
        return CompletionItemKind.Unknown;
    }

    private static bool ListsEqual(IReadOnlyList<string>? a, IReadOnlyList<string>? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.SequenceEqual(b);
    }
}
