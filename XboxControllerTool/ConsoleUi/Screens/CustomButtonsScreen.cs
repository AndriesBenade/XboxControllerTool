using XboxControllerTool.Application;

namespace XboxControllerTool.ConsoleUi.Screens;

public sealed class CustomButtonsScreen : ListMenuScreen
{
    private const int LabelWidth = 14;

    private readonly CustomButtonService _customButtons;

    public CustomButtonsScreen(CustomButtonService customButtons)
    {
        _customButtons = customButtons;
    }

    protected override int ItemCount => _customButtons.DetectedButtons.Count;

    public override IReadOnlyList<ConsoleLine> BuildLines()
    {
        var buttons = _customButtons.DetectedButtons;
        var lines = new List<ConsoleLine>(AppShell.Header());

        lines.Add(Panel.Top("CUSTOM BUTTONS"));
        LayoutMetrics.PanelPad(lines);

        for (var i = 0; i < buttons.Count; i++)
        {
            var button = buttons[i];

            lines.Add(MenuItemRow.BuildSetting(
                $"[ {button.Label} ]",
                SelectedIndex == i,
                [new ConsoleSegment(_customButtons.DescribeMapping(button.Mask), MappingColour(button.Mask))],
                LabelWidth,
                ControllerButton.A));
        }

        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Section("DETECTION"));
        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Row(new ConsoleSegment("Press any spare controller button to add it to this list.", ConsoleTheme.Text)));
        lines.Add(Panel.Row(new ConsoleSegment("Buttons already used by built-in actions cannot be remapped.", ConsoleTheme.Label)));
        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Bottom());

        lines.AddRange(AppShell.Footer(
            (ControllerButton.A, "Map"),
            (ControllerButton.X, "Clear"),
            (ControllerButton.UpDown, "Navigate"),
            (ControllerButton.B, "Back")));

        return lines;
    }

    protected override NavigationCommand OnConfirm(int index)
    {
        var buttons = _customButtons.DetectedButtons;

        return index < buttons.Count
            ? NavigationCommand.Push(new ButtonMappingScreen(_customButtons, buttons[index].Mask))
            : NavigationCommand.None;
    }

    public override NavigationCommand HandleAction(MenuAction action)
    {
        // X clears the highlighted mapping without leaving the list.
        if (action == MenuAction.Clear)
        {
            var buttons = _customButtons.DetectedButtons;

            if (SelectedIndex < buttons.Count)
            {
                _customButtons.Clear(buttons[SelectedIndex].Mask);
            }

            return NavigationCommand.None;
        }

        return base.HandleAction(action);
    }

    private ConsoleColor MappingColour(ushort mask) =>
        _customButtons.FindMapping(mask) is null ? ConsoleTheme.Label : ConsoleTheme.Value;
}
