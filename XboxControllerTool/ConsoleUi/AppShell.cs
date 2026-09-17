using System.Reflection;

namespace XboxControllerTool.ConsoleUi;

public static class AppShell
{
    private const string SpacedTitle = "X B O X   C O N T R O L L E R   T O O L";
    private const string Author = "by Andries Benade";

    public static readonly string VersionText = "v" + (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0");

    public static IReadOnlyList<ConsoleLine> Header() =>
    [
        Band(Glyphs.BandTopLeft, Glyphs.BandTopRight),
        BandRow([new ConsoleSegment(SpacedTitle, ConsoleTheme.Title)], [new ConsoleSegment(VersionText, ConsoleTheme.Label)]),
        BandRow([new ConsoleSegment(Author, ConsoleTheme.Label)]),
        Band(Glyphs.BandBottomLeft, Glyphs.BandBottomRight),
        ConsoleLine.Empty
    ];

    public static IReadOnlyList<ConsoleLine> Footer(params (string Button, string Label)[] hints) =>
    [
        ConsoleLine.Empty,
        HintBar.Build(hints)
    ];

    private static ConsoleLine Band(char left, char right) => new(
        new ConsoleSegment($"{left}{new string(Glyphs.BandHorizontal, Panel.Width - 2)}{right}", ConsoleTheme.BandFrame));

    private static ConsoleLine BandRow(ConsoleSegment[] left, ConsoleSegment[]? right = null)
    {
        var leftLength = left.Sum(segment => segment.Text.Length);
        var rightLength = right?.Sum(segment => segment.Text.Length) ?? 0;
        var pad = Math.Max(Panel.ContentWidth - leftLength - rightLength, 1);

        var segments = new List<ConsoleSegment>(left.Length + (right?.Length ?? 0) + 3)
        {
            new($"{Glyphs.BandVertical}  ", ConsoleTheme.BandFrame)
        };

        segments.AddRange(left);
        segments.Add(new ConsoleSegment(new string(' ', pad)));

        if (right is not null)
        {
            segments.AddRange(right);
        }

        segments.Add(new ConsoleSegment($"  {Glyphs.BandVertical}", ConsoleTheme.BandFrame));

        return new ConsoleLine(segments.ToArray());
    }
}
