using QuickNET.Models;
using QuickNET.Session;

namespace QuickNET.MetaCommands;

public class MetaCommandService
{
    private readonly SessionState _sessionState;

    public MetaCommandService(SessionState sessionState)
    {
        _sessionState = sessionState;
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
            "reference" => NotYetImplemented(command),
            "import" or "using" => NotYetImplemented(command),
            "references" => NotYetImplemented(command),
            "imports" => NotYetImplemented(command),
            "timeout" => NotYetImplemented(command),
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
