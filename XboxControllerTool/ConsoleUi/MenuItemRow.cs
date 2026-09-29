namespace XboxControllerTool.ConsoleUi;

public static class MenuItemRow
{
    private const int FocusBarWidth = 3;
    private const int BadgeWidth = 7;

    public static ConsoleLine Build(string label, string description, bool focused, int labelWidth, string badgeButton = ControllerButton.A, int itemIndex = -1)
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

        return Panel.Row(segments.ToArray()).ForItem(itemIndex);
    }

    public static ConsoleLine BuildSetting(string label, bool focused, ConsoleSegment[] control, int labelWidth, string badgeButton, int itemIndex = -1)
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

        return Panel.Row(segments.ToArray())
            .ForItem(itemIndex)
            .WithRegions(AdjustRegions(badgeButton, labelWidth));
    }

    /// <summary>
    /// Splits the control area of an adjustable row in two, so clicking its left half decreases and
    /// its right half increases - the halves the on-screen arrows sit in. A row driven by A instead
    /// gets no regions, because a plain click on it already activates it.
    /// </summary>
    private static IReadOnlyList<ClickRegion> AdjustRegions(string badgeButton, int labelWidth)
    {
        if (badgeButton != ControllerButton.LeftRight)
        {
            return [];
        }

        var start = Panel.RowContentColumn + FocusBarWidth + labelWidth;
        var width = Panel.ContentWidth - FocusBarWidth - labelWidth - BadgeWidth;

        if (width < 2)
        {
            return [];
        }

        var middle = start + (width / 2);

        return
        [
            new ClickRegion(start, middle - 1, MenuAction.Left),
            new ClickRegion(middle, start + width - 1, MenuAction.Right)
        ];
    }
}
