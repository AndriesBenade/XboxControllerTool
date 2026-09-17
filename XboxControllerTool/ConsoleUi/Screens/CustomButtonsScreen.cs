using XboxControllerTool.Application;

namespace XboxControllerTool.ConsoleUi.Screens;

public sealed class CustomButtonsScreen : ListMenuScreen
{
    private const int LabelWidth = 16;
    private const int DetectItemIndex = 0;

    private readonly CustomButtonService _customButtons;

    public CustomButtonsScreen(CustomButtonService customButtons)
    {
        _customButtons = customButtons;
    }

    protected override int ItemCount => _customButtons.DetectedButtons.Count + 1;

    public override IReadOnlyList<ConsoleLine> BuildLines()
    {
        var buttons = _customButtons.DetectedButtons;
        var lines = new List<ConsoleLine>(AppShell.Header());

        lines.Add(Panel.Top("CUSTOM BUTTONS"));
        LayoutMetrics.PanelPad(lines);

        lines.Add(MenuItemRow.BuildSetting(
            "Detect Button",
            SelectedIndex == DetectItemIndex,
            SettingControl.Action("Press a spare button to add it"),
            LabelWidth,
            ControllerButton.A));

        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Section("DETECTED BUTTONS"));
        LayoutMetrics.PanelPad(lines);

        if (buttons.Count == 0)
        {
            lines.Add(Panel.Row(new ConsoleSegment("No spare buttons have been detected yet.", ConsoleTheme.Label)));
        }

        for (var i = 0; i < buttons.Count; i++)
        {
            var button = buttons[i];

            lines.Add(MenuItemRow.BuildSetting(
                $"[ {button.Label} ]",
                SelectedIndex == i + 1,
                [new ConsoleSegment(_customButtons.DescribeMapping(button.Id), MappingColour(button.Id))],
                LabelWidth,
                ControllerButton.A));
        }

        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Section("DETECTION"));
        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Row(new ConsoleSegment("A button appears here only after Windows has reported a", ConsoleTheme.Text)));
        lines.Add(Panel.Row(new ConsoleSegment("real press from it, and must be pressed again this session", ConsoleTheme.Text)));
        lines.Add(Panel.Row(new ConsoleSegment("before its mapping can be changed.", ConsoleTheme.Text)));
        lines.Add(Panel.Row(new ConsoleSegment("Buttons already used by built-in actions cannot be remapped.", ConsoleTheme.Label)));
        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Bottom());

        lines.AddRange(AppShell.Footer(
            (ControllerButton.A, "Select"),
            (ControllerButton.X, "Clear"),
            (ControllerButton.UpDown, "Navigate"),
            (ControllerButton.B, "Back")));

        return lines;
    }

    protected override NavigationCommand OnConfirm(int index)
    {
        if (index == DetectItemIndex)
        {
            return NavigationCommand.Push(new ButtonDetectionScreen(_customButtons));
        }

        var buttons = _customButtons.DetectedButtons;
        var buttonIndex = index - 1;

        return buttonIndex < buttons.Count
            ? NavigationCommand.Push(new ButtonMappingScreen(_customButtons, buttons[buttonIndex].Id))
            : NavigationCommand.None;
    }

    public override NavigationCommand HandleAction(MenuAction action)
    {
        // X clears the highlighted mapping without leaving the list.
        if (action == MenuAction.Clear)
        {
            var buttons = _customButtons.DetectedButtons;
            var buttonIndex = SelectedIndex - 1;

            if (buttonIndex >= 0 && buttonIndex < buttons.Count)
            {
                _customButtons.Clear(buttons[buttonIndex].Id);
            }

            return NavigationCommand.None;
        }

        return base.HandleAction(action);
    }

    private ConsoleColor MappingColour(string buttonId) =>
        _customButtons.FindMapping(buttonId) is null ? ConsoleTheme.Label : ConsoleTheme.Value;
}
