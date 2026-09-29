namespace XboxControllerTool.ConsoleUi;

public static class HintBar
{
    /// <summary>
    /// The hints that do something when clicked. Back is the important one: without it there is no
    /// way out of a screen with a mouse, since nothing else on the screen goes backwards.
    /// </summary>
    private static MenuAction ActionFor(string button) => button switch
    {
        ControllerButton.A => MenuAction.Confirm,
        ControllerButton.B => MenuAction.Cancel,
        ControllerButton.X => MenuAction.Clear,
        _ => MenuAction.None
    };

    public static ConsoleLine Build(params (string Button, string Label)[] hints)
    {
        var segments = new List<ConsoleSegment> { new("   ") };
        var regions = new List<ClickRegion>();
        var column = segments[0].Text.Length;

        for (var i = 0; i < hints.Length; i++)
        {
            if (i > 0)
            {
                var gap = new ConsoleSegment("     ");
                segments.Add(gap);
                column += gap.Text.Length;
            }

            var start = column;

            var badge = ControllerButton.Render(hints[i].Button);
            segments.AddRange(badge);
            column += badge.Sum(segment => segment.Text.Length);

            var label = new ConsoleSegment(" " + hints[i].Label, ConsoleTheme.Text);
            segments.Add(label);
            column += label.Text.Length;

            var action = ActionFor(hints[i].Button);

            if (action != MenuAction.None)
            {
                regions.Add(new ClickRegion(start, column - 1, action));
            }
        }

        return new ConsoleLine(segments.ToArray()) { Regions = regions };
    }
}
