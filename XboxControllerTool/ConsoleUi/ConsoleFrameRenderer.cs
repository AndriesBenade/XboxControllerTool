namespace XboxControllerTool.ConsoleUi;

public sealed class ConsoleFrameRenderer
{
    private ConsoleLine[] _previous = [];

    public void Render(IReadOnlyList<ConsoleLine> lines)
    {
        var width = Math.Max(Console.WindowWidth - 1, 40);

        if (lines.Count != _previous.Length)
        {
            Console.Clear();
            _previous = new ConsoleLine[lines.Count];
        }

        for (var row = 0; row < lines.Count; row++)
        {
            if (row < _previous.Length && lines[row].Equals(_previous[row]))
            {
                continue;
            }

            WriteLine(row, lines[row], width);
        }

        _previous = [.. lines];
    }

    public void Invalidate() => _previous = [];

    private static void WriteLine(int row, ConsoleLine line, int width)
    {
        Console.SetCursorPosition(0, row);

        var originalForeground = Console.ForegroundColor;
        var originalBackground = Console.BackgroundColor;
        var written = 0;

        foreach (var segment in line.Segments)
        {
            if (written >= width)
            {
                break;
            }

            var remaining = width - written;
            var text = segment.Text.Length > remaining ? segment.Text[..remaining] : segment.Text;

            Console.ForegroundColor = segment.Foreground ?? ConsoleTheme.Text;
            Console.BackgroundColor = segment.Background ?? ConsoleTheme.Background;
            Console.Write(text);
            written += text.Length;
        }

        if (written < width)
        {
            Console.ForegroundColor = ConsoleTheme.Text;
            Console.BackgroundColor = ConsoleTheme.Background;
            Console.Write(new string(' ', width - written));
        }

        Console.ForegroundColor = originalForeground;
        Console.BackgroundColor = originalBackground;
    }
}
