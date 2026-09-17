namespace XboxControllerTool.ConsoleUi;

public static class HintBar
{
    public static ConsoleLine Build(params (string Button, string Label)[] hints)
    {
        var segments = new List<ConsoleSegment> { new("   ") };

        for (var i = 0; i < hints.Length; i++)
        {
            if (i > 0)
            {
                segments.Add(new ConsoleSegment("     "));
            }

            segments.AddRange(ControllerButton.Render(hints[i].Button));
            segments.Add(new ConsoleSegment(" " + hints[i].Label, ConsoleTheme.Text));
        }

        return new ConsoleLine(segments.ToArray());
    }
}
