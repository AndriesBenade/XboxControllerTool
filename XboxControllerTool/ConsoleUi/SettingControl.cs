namespace XboxControllerTool.ConsoleUi;

public static class SettingControl
{
    private const int GaugeWidth = 12;

    public static ConsoleSegment[] Gauge(double value, double min, double max, string displayText, bool focused)
    {
        var ratio = Math.Clamp((value - min) / (max - min), 0.0, 1.0);
        var filled = (int)Math.Round(ratio * GaugeWidth);

        return
        [
            new ConsoleSegment(new string(Glyphs.GaugeFilled, filled), focused ? ConsoleTheme.Focus : ConsoleTheme.Text),
            new ConsoleSegment(new string(Glyphs.GaugeEmpty, GaugeWidth - filled), ConsoleTheme.PanelFrame),
            new ConsoleSegment("  " + displayText.PadLeft(5), ConsoleTheme.Value)
        ];
    }

    public static ConsoleSegment[] Toggle(bool value) =>
    [
        value
            ? new ConsoleSegment("[ ON  ]", ConsoleTheme.Positive)
            : new ConsoleSegment("[ OFF ]", ConsoleTheme.Label)
    ];

    public static ConsoleSegment[] Cycle(string value) =>
    [
        new ConsoleSegment("<  ", ConsoleTheme.Label),
        new ConsoleSegment(value, ConsoleTheme.Value),
        new ConsoleSegment("  >", ConsoleTheme.Label)
    ];

    public static ConsoleSegment[] Action(string description) =>
    [
        new ConsoleSegment(description, ConsoleTheme.Active)
    ];
}
