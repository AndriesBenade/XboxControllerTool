namespace XboxControllerTool.ConsoleUi;

/// <summary>
/// Draws a screen by writing only the rows that changed since the previous frame.
/// <para>
/// Every console call here is treated as able to fail. The window can be resized, or its handle
/// taken away, at any moment between two frames, and a drawing failure must never be allowed to
/// bring the application down - the worst acceptable outcome is a frame that does not appear.
/// </para>
/// </summary>
public sealed class ConsoleFrameRenderer
{
    private const int FallbackWidth = 80;
    private const int FallbackHeight = 25;
    private const int MinimumWidth = 40;

    private ConsoleLine[] _previous = [];

    public void Render(IReadOnlyList<ConsoleLine> lines)
    {
        var width = Math.Max(Measure(() => Console.WindowWidth, FallbackWidth) - 1, MinimumWidth);

        // A screen taller than the buffer is clipped rather than allowed to throw, because the
        // console can be resized below the height the layout was sized for.
        var bufferHeight = Math.Max(Measure(() => Console.BufferHeight, FallbackHeight), 0);
        var visibleRows = Math.Min(lines.Count, bufferHeight);

        if (visibleRows != _previous.Length)
        {
            if (!Try(Console.Clear))
            {
                return;
            }

            _previous = new ConsoleLine[visibleRows];
        }

        for (var row = 0; row < visibleRows; row++)
        {
            if (row < _previous.Length && lines[row].Equals(_previous[row]))
            {
                continue;
            }

            if (!Try(() => WriteLine(row, lines[row], width)))
            {
                // The console changed underneath this frame; drop what is left and redraw next tick.
                _previous = [];
                return;
            }
        }

        _previous = [.. lines.Take(visibleRows)];
    }

    public void Invalidate() => _previous = [];

    private static int Measure(Func<int> read, int fallback)
    {
        try
        {
            return read();
        }
        catch (Exception ex) when (ex is IOException or PlatformNotSupportedException)
        {
            return fallback;
        }
    }

    private static bool Try(Action write)
    {
        try
        {
            write();
            return true;
        }
        catch (Exception ex) when (ex is IOException or ArgumentOutOfRangeException or PlatformNotSupportedException)
        {
            return false;
        }
    }

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
