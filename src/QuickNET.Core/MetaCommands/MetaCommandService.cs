using System.Text;
using QuickNET.Compilation;
using QuickNET.Models;
using QuickNET.Session;

namespace QuickNET.MetaCommands;

public class MetaCommandService
{
    private readonly SessionState _sessionState;
    private readonly AssemblyResolutionService _assemblyResolver;

    public MetaCommandService(SessionState sessionState, AssemblyResolutionService assemblyResolver)
    {
        _sessionState = sessionState;
        _assemblyResolver = assemblyResolver;
    }

    public MetaCommandResult Execute(string input)
    {
        var (command, args) = MetaCommandParser.Parse(input);

        if (string.IsNullOrEmpty(command))
            return new MetaCommandResult
            {
                Command = "",
                DisplayText = "Not a meta-command.",
                Success = false
            };

        return command switch
        {
            "clear" => ExecuteClear(),
            "help" => ExecuteHelp(),
            "lang" => ExecuteLang(args),
            "reference" => ExecuteReference(args),
            "import" or "using" => ExecuteImport(args),
            "references" => ExecuteReferences(),
            "imports" => ExecuteImports(),
            "timeout" => ExecuteTimeout(args),
            "exit" => ExecuteExit(),
            _ => new MetaCommandResult
            {
                Command = command,
                DisplayText = $"Unknown command '/{command}'. Type /help for available commands.",
                Success = false
            }
        };
    }

    private MetaCommandResult ExecuteClear()
    {
        return new MetaCommandResult
        {
            Command = "clear",
            DisplayText = "Conversation cleared.",
            Success = true
        };
    }

    private MetaCommandResult ExecuteHelp()
    {
        var help = """
            Available commands:
              /clear                 Clear the conversation panel and history
              /exit                  Exit the application
              /help                  Show this help message
              /lang <cs|vb>          Switch language (cs = C#, vb = VB.NET)
              /reference <assembly>  Add an assembly reference
              /import <namespace>    Add a namespace import (alias: /using)
              /references            List all referenced assemblies
              /imports               List all imported namespaces
              /timeout <seconds>     Set execution timeout (0 = no limit)
            """;
        return new MetaCommandResult
        {
            Command = "help",
            DisplayText = help,
            Success = true
        };
    }

    private MetaCommandResult ExecuteLang(string? args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new MetaCommandResult
            {
                Command = "lang",
                DisplayText = $"Current language: {_sessionState.CurrentLanguage}. Usage: /lang <cs|vb>",
                Success = false
            };

        var lang = args.Trim().ToLowerInvariant();
        if (lang == "cs" || lang == "csharp" || lang == "c#")
        {
            _sessionState.CurrentLanguage = Language.CSharp;
            return new MetaCommandResult
            {
                Command = "lang",
                DisplayText = "Language set to C#.",
                Success = true
            };
        }

        if (lang == "vb" || lang == "vbnet" || lang == "vb.net" || lang == "visualbasic")
        {
            _sessionState.CurrentLanguage = Language.VisualBasic;
            return new MetaCommandResult
            {
                Command = "lang",
                DisplayText = "Language set to VB.NET.",
                Success = true
            };
        }

        return new MetaCommandResult
        {
            Command = "lang",
            DisplayText = $"Unknown language '{args}'. Use 'cs' for C# or 'vb' for VB.NET.",
            Success = false
        };
    }

    private MetaCommandResult ExecuteReference(string? args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new MetaCommandResult
            {
                Command = "reference",
                DisplayText = "Usage: /reference <assembly_name>\nExample: /reference System.Text.Json",
                Success = false
            };

        var assemblyName = args.Trim();
        var resolved = _assemblyResolver.Resolve(assemblyName);

        if (resolved == null)
            return new MetaCommandResult
            {
                Command = "reference",
                DisplayText = $"Assembly '{assemblyName}' not found in the runtime.",
                Success = false
            };

        _sessionState.AddReference(assemblyName);
        return new MetaCommandResult
        {
            Command = "reference",
            DisplayText = $"Added reference to '{assemblyName}'.",
            Success = true
        };
    }

    private MetaCommandResult ExecuteImport(string? args)
    {
        if (string.IsNullOrWhiteSpace(args))
            return new MetaCommandResult
            {
                Command = "import",
                DisplayText = "Usage: /import <namespace> (alias: /using)\nExample: /import System.Text.Json",
                Success = false
            };

        var ns = args.Trim();
        _sessionState.AddImport(ns);
        return new MetaCommandResult
        {
            Command = "import",
            DisplayText = $"Added import for '{ns}'.",
            Success = true
        };
    }

    private MetaCommandResult ExecuteReferences()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Default references:");
        sb.AppendLine("  System.Console");
        sb.AppendLine("  System.IO.FileSystem");
        sb.AppendLine("  System.Linq");
        sb.AppendLine("  System.Runtime");
        sb.AppendLine("  System.Text.Encoding");
        sb.AppendLine("  System.Threading.Tasks");
        sb.AppendLine();

        var extraRefs = _sessionState.ExtraReferences;
        if (extraRefs.Count > 0)
        {
            sb.AppendLine("Extra references (via /reference):");
            foreach (var r in extraRefs)
                sb.AppendLine($"  {r}");
        }
        else
        {
            sb.AppendLine("No extra references added.");
        }

        return new MetaCommandResult
        {
            Command = "references",
            DisplayText = sb.ToString().TrimEnd(),
            Success = true
        };
    }

    private MetaCommandResult ExecuteImports()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Default imports:");
        sb.AppendLine("  System");
        sb.AppendLine("  System.Collections.Generic");
        sb.AppendLine("  System.IO");
        sb.AppendLine("  System.Linq");
        sb.AppendLine("  System.Text");
        sb.AppendLine("  System.Threading.Tasks");
        sb.AppendLine();

        var extraImports = _sessionState.ExtraImports;
        if (extraImports.Count > 0)
        {
            sb.AppendLine("Extra imports (via /import):");
            foreach (var imp in extraImports)
                sb.AppendLine($"  {imp}");
        }
        else
        {
            sb.AppendLine("No extra imports added.");
        }

        return new MetaCommandResult
        {
            Command = "imports",
            DisplayText = sb.ToString().TrimEnd(),
            Success = true
        };
    }

    private MetaCommandResult ExecuteTimeout(string? args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            var currentSecs = _sessionState.TimeoutSeconds;
            var currentLabel = currentSecs == 0 ? "no limit" : $"{currentSecs}s";
            return new MetaCommandResult
            {
                Command = "timeout",
                DisplayText = $"Current timeout: {currentLabel}. Usage: /timeout <seconds> (0 = no limit)",
                Success = true
            };
        }

        var trimmed = args.Trim();
        if (!int.TryParse(trimmed, out var seconds) || seconds < 0)
        {
            return new MetaCommandResult
            {
                Command = "timeout",
                DisplayText = $"Invalid timeout value '{trimmed}'. Expected a non-negative number (0 = no limit).",
                Success = false
            };
        }

        _sessionState.TimeoutSeconds = seconds;
        var label = seconds == 0 ? "no limit" : $"{seconds}s";
        return new MetaCommandResult
        {
            Command = "timeout",
            DisplayText = $"Execution timeout set to {label}.",
            Success = true
        };
    }

    private MetaCommandResult ExecuteExit()
    {
        return new MetaCommandResult
        {
            Command = "exit",
            DisplayText = "Goodbye!",
            Success = true
        };
    }

    private static MetaCommandResult NotYetImplemented(string command)
    {
        return new MetaCommandResult
        {
            Command = command,
            DisplayText = $"Command '/{command}' will be available in the next task.",
            Success = false
        };
    }
}
