using System.Text;

namespace QuickNET.Execution;

internal class CaptureTextWriter : TextWriter
{
    private readonly StringBuilder _sb = new();
    private readonly TextWriter _original;

    public CaptureTextWriter(TextWriter original)
    {
        _original = original;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        _original.Write(value);
        _sb.Append(value);
    }

    public override void Write(string? value)
    {
        _original.Write(value);
        _sb.Append(value);
    }

    public override void WriteLine(string? value)
    {
        _original.WriteLine(value);
        _sb.AppendLine(value);
    }

    public string GetOutput() => _sb.ToString();
}
