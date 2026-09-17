namespace XboxControllerTool.ConsoleUi;

public static class Panel
{
    public const int Width = 78;
    public const int ContentWidth = Width - 6;

    public static ConsoleLine Top(string title) => Frame(Glyphs.TopLeft, Glyphs.TopRight, title);

    public static ConsoleLine Section(string title) => Frame(Glyphs.TeeLeft, Glyphs.TeeRight, title);

    public static ConsoleLine Bottom() => new(
        new ConsoleSegment($"{Glyphs.BottomLeft}{new string(Glyphs.Horizontal, Width - 2)}{Glyphs.BottomRight}", ConsoleTheme.PanelFrame));

    public static ConsoleLine Blank() => Row();

    public static ConsoleLine Row(params ConsoleSegment[] content)
    {
        var used = content.Sum(segment => segment.Text.Length);
        var pad = Math.Max(ContentWidth - used, 0);

        var segments = new List<ConsoleSegment>(content.Length + 2)
        {
            new($"{Glyphs.Vertical}  ", ConsoleTheme.PanelFrame)
        };

        segments.AddRange(content);
        segments.Add(new ConsoleSegment($"{new string(' ', pad)}  {Glyphs.Vertical}", ConsoleTheme.PanelFrame));

        return new ConsoleLine(segments.ToArray());
    }

    private static ConsoleLine Frame(char left, char right, string title)
    {
        var trailing = Math.Max(Width - 5 - title.Length, 1);

        return new ConsoleLine(
            new ConsoleSegment($"{left}{Glyphs.Horizontal} ", ConsoleTheme.PanelFrame),
            new ConsoleSegment(title, ConsoleTheme.PanelTitle),
            new ConsoleSegment($" {new string(Glyphs.Horizontal, trailing)}{right}", ConsoleTheme.PanelFrame));
    }
}
