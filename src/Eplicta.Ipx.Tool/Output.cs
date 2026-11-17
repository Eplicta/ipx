namespace Eplicta.Ipx.Tool;

/// <summary>Writes text, in colour when the output is a terminal that shows it.</summary>
internal sealed class Output
{
    private const string Reset = "\u001b[0m";

    private readonly bool _color;

    public Output(TextWriter output, TextWriter error, bool color)
    {
        Out = output;
        Error = error;
        _color = color;
    }

    public TextWriter Out { get; }

    public TextWriter Error { get; }

    public void Write(string text, OutputColor color = OutputColor.Default)
    {
        if (!_color || color == OutputColor.Default)
        {
            Out.Write(text);
            return;
        }

        Out.Write(Code(color) + text + Reset);
    }

    public void Line(string text = "", OutputColor color = OutputColor.Default)
    {
        Write(text, color);
        Out.WriteLine();
    }

    /// <summary>The text cut to the width with an ellipsis, or padded with spaces to it, on one line.</summary>
    public static string Fit(string text, int width)
    {
        var flat = string.Join(' ', (text ?? string.Empty).Split((char[])null, StringSplitOptions.RemoveEmptyEntries));
        return flat.Length > width ? flat[..Math.Max(0, width - 1)] + "…" : flat.PadRight(width);
    }

    private static string Code(OutputColor color) => color switch
    {
        OutputColor.Green => "\u001b[32m",
        OutputColor.Yellow => "\u001b[93m",
        OutputColor.DarkYellow => "\u001b[33m",
        OutputColor.Magenta => "\u001b[35m",
        OutputColor.Cyan => "\u001b[36m",
        OutputColor.Red => "\u001b[31m",
        OutputColor.Gray => "\u001b[90m",
        OutputColor.White => "\u001b[97m",
        _ => string.Empty
    };
}
