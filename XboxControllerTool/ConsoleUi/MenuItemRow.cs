namespace XboxControllerTool.ConsoleUi;

public static class MenuItemRow
{
    private const int FocusBarWidth = 3;
    private const int BadgeWidth = 7;

    public static ConsoleLine Build(string label, string description, bool focused, int labelWidth, string badgeButton = ControllerButton.A)
    {
        var descriptionWidth = Panel.ContentWidth - FocusBarWidth - labelWidth - BadgeWidth;

        var segments = new List<ConsoleSegment>
        {
            focused
                ? new ConsoleSegment($"{Glyphs.FocusBar}{Glyphs.FocusBar} ", ConsoleTheme.Focus)
                : new ConsoleSegment(new string(' ', FocusBarWidth)),
            new(label.PadRight(labelWidth), focused ? ConsoleTheme.Focus : ConsoleTheme.Text),
            new(description.PadRight(descriptionWidth), focused ? ConsoleTheme.Text : ConsoleTheme.Label)
        };

        segments.AddRange(focused
            ? ControllerButton.Badge(badgeButton, BadgeWidth)
            : ControllerButton.BadgeSpacer(BadgeWidth));

        return Panel.Row(segments.ToArray());
    }

    public static ConsoleLine BuildSetting(string label, bool focused, ConsoleSegment[] control, int labelWidth, string badgeButton)
    {
        var controlWidth = Panel.ContentWidth - FocusBarWidth - labelWidth - BadgeWidth;
        var controlLength = control.Sum(segment => segment.Text.Length);

        var segments = new List<ConsoleSegment>
        {
            focused
                ? new ConsoleSegment($"{Glyphs.FocusBar}{Glyphs.FocusBar} ", ConsoleTheme.Focus)
                : new ConsoleSegment(new string(' ', FocusBarWidth)),
            new(label.PadRight(labelWidth), focused ? ConsoleTheme.Focus : ConsoleTheme.Text)
        };

        segments.AddRange(control);
        segments.Add(new ConsoleSegment(new string(' ', Math.Max(controlWidth - controlLength, 1))));

        segments.AddRange(focused
            ? ControllerButton.Badge(badgeButton, BadgeWidth)
            : ControllerButton.BadgeSpacer(BadgeWidth));

        return Panel.Row(segments.ToArray());
    }
}
