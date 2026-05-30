namespace QuickNET.App.Completion;

public static class TriggerHelper
{
    public static bool ShouldAutoTrigger(string text, int position)
    {
        if (position <= 0 || position > text.Length) return false;

        return text[position - 1] == '.';
    }
}
