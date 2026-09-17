using XboxControllerTool.Application;

namespace XboxControllerTool.ConsoleUi.Screens;

public sealed record MenuDestination(string Label, string Description, IScreen? Screen);

public sealed class HomeScreen : ListMenuScreen
{
    private const int StatusLabelWidth = 12;
    private const int StatusValueWidth = 24;
    private const int MenuLabelWidth = 14;
    private const int ButtonBadgeWidth = 10;
    private const int ButtonActionWidth = 14;
    private const int StickBadgeWidth = 12;
    private const int StickActionWidth = 24;

    /// <summary>Caps how many custom mappings the dashboard lists, so its height stays bounded.</summary>
    private const int MaxMappedRows = 3;

    private static readonly (string Button, string Action)[] ControlMap =
    [
        (ControllerButton.A, "Left Click"),
        (ControllerButton.Start, "Browser"),
        (ControllerButton.LeftTrigger, "Precision"),

        (ControllerButton.X, "Right Click"),
        (ControllerButton.Back, "Browser Back"),
        (ControllerButton.RightTrigger, "Enter"),

        (ControllerButton.B, "Backspace"),
        (ControllerButton.LeftBumper, "Show Desktop"),
        (ControllerButton.RightBumper, "Escape"),

        (ControllerButton.Up, "Keyboard"),
        (ControllerButton.Down, "Voice Input"),
        (ControllerButton.RightStickClick, "Middle Click")
    ];

    private readonly AppState _appState;
    private readonly CustomButtonService _customButtons;
    private readonly IReadOnlyList<MenuDestination> _destinations;

    public HomeScreen(AppState appState, CustomButtonService customButtons, IReadOnlyList<MenuDestination> destinations)
    {
        _appState = appState;
        _customButtons = customButtons;
        _destinations = destinations;
    }

    protected override int ItemCount => _destinations.Count;

    public override IReadOnlyList<ConsoleLine> BuildLines()
    {
        var lines = new List<ConsoleLine>(AppShell.Header());

        lines.Add(Panel.Top("STATUS"));
        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Row(
        [
            .. FieldRow.Build("CONTROLLER", StatusLabelWidth, AppStateFormatting.Connection(_appState), StatusValueWidth),
            .. FieldRow.Build("MODE", StatusLabelWidth, AppStateFormatting.Mode(_appState))
        ]));
        lines.Add(Panel.Row(
        [
            .. FieldRow.Build("SELECTED", StatusLabelWidth, AppStateFormatting.SelectedPad(_appState), StatusValueWidth),
            .. FieldRow.Build("SPEED", StatusLabelWidth, AppStateFormatting.Speed(_appState))
        ]));
        lines.Add(Panel.Row(
        [
            .. FieldRow.Build("DESKTOP", StatusLabelWidth, AppStateFormatting.DesktopInput(_appState), StatusValueWidth),
            .. FieldRow.Build("VOICE", StatusLabelWidth, AppStateFormatting.OnOff(_appState.VoiceInputActive))
        ]));
        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Bottom());
        LayoutMetrics.Gap(lines);

        lines.Add(Panel.Top("CONTROLS"));
        LayoutMetrics.PanelPad(lines);

        if (!LayoutMetrics.Compact)
        {
            lines.Add(Panel.Row(
            [
                .. ControllerButton.Cell(ControllerButton.LeftStick, "Move Cursor", StickBadgeWidth, StickActionWidth),
                .. ControllerButton.Cell(ControllerButton.RightStick, "Scroll", StickBadgeWidth, StickActionWidth)
            ]));
            lines.Add(Panel.Blank());
        }

        for (var row = 0; row < ControlMap.Length; row += 3)
        {
            var cells = new List<ConsoleSegment>();

            for (var column = 0; column < 3 && row + column < ControlMap.Length; column++)
            {
                var (button, action) = ControlMap[row + column];
                cells.AddRange(ControllerButton.Cell(button, action, ButtonBadgeWidth, ButtonActionWidth));
            }

            lines.Add(Panel.Row(cells.ToArray()));
        }

        var mapped = _customButtons.DetectedButtons
            .Where(button => _customButtons.FindMapping(button.Id) is not null)
            .ToList();

        if (mapped.Count > 0)
        {
            lines.Add(Panel.Blank());

            // The dashboard has a fixed row budget, so a long mapping list is summarised here
            // rather than pushing the menu off the bottom of the window.
            var shown = mapped.Count > MaxMappedRows ? MaxMappedRows - 1 : mapped.Count;

            foreach (var button in mapped.Take(shown))
            {
                lines.Add(Panel.Row(
                [
                    .. ControllerButton.Cell(button.Label, _customButtons.DescribeMapping(button.Id), ButtonBadgeWidth, Panel.ContentWidth - ButtonBadgeWidth)
                ]));
            }

            if (mapped.Count > shown)
            {
                lines.Add(Panel.Row(new ConsoleSegment(
                    $"+ {mapped.Count - shown} more in Settings > Custom Buttons", ConsoleTheme.Label)));
            }
        }

        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Bottom());
        LayoutMetrics.Gap(lines);

        lines.Add(Panel.Top("MENU"));
        LayoutMetrics.PanelPad(lines);

        for (var i = 0; i < _destinations.Count; i++)
        {
            lines.Add(MenuItemRow.Build(
                _destinations[i].Label,
                _destinations[i].Description,
                focused: i == SelectedIndex,
                MenuLabelWidth));
        }

        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Bottom());

        lines.AddRange(AppShell.Footer(
            (ControllerButton.A, "Select"),
            (ControllerButton.UpDown, "Navigate"),
            (ControllerButton.Y, "Hide App")));

        return lines;
    }

    protected override NavigationCommand OnConfirm(int index)
    {
        var destination = _destinations[index];

        return destination.Screen is null
            ? NavigationCommand.Exit
            : NavigationCommand.Push(destination.Screen);
    }

    protected override NavigationCommand OnCancel() => NavigationCommand.None;
}
