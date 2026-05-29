namespace QuickNET.MetaCommands;

public static class MetaCommandParser
{
    public static bool IsMetaCommand(string input)
    {
        return !string.IsNullOrWhiteSpace(input) && input.TrimStart().StartsWith('/');
    }

    public static (string Command, string? Args) Parse(string input)
    {
        var trimmed = input.TrimStart();
        if (!trimmed.StartsWith('/'))
            return ("", null);

        var spaceIndex = trimmed.IndexOf(' ');
        if (spaceIndex < 0)
            return (trimmed[1..].ToLowerInvariant(), null);

        var command = trimmed[1..spaceIndex].ToLowerInvariant();
        var args = trimmed[(spaceIndex + 1)..].Trim();
        return (command, args.Length > 0 ? args : null);
    }
}
