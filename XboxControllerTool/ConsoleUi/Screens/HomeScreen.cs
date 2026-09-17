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

    private static readonly (string Button, string Action)[] ControlMap =
    [
        (ControllerButton.A, "Left Click"),
        (ControllerButton.Start, "Browser"),
        (ControllerButton.LeftTrigger, "Precision"),

        (ControllerButton.X, "Right Click"),
        (ControllerButton.Back, "Browser Back"),
        (ControllerButton.RightTrigger, "Fast Speed"),

        (ControllerButton.B, "Backspace"),
        (ControllerButton.LeftBumper, "Show Desktop"),
        (ControllerButton.LeftStickClick, "Enter"),

        (ControllerButton.Up, "Keyboard"),
        (ControllerButton.Down, "Voice Input"),
        (ControllerButton.RightBumper, "Escape")
    ];

    private readonly AppState _appState;
    private readonly IReadOnlyList<MenuDestination> _destinations;

    public HomeScreen(AppState appState, IReadOnlyList<MenuDestination> destinations)
    {
        _appState = appState;
        _destinations = destinations;
    }

    protected override int ItemCount => _destinations.Count;

    public override IReadOnlyList<ConsoleLine> BuildLines()
    {
        var lines = new List<ConsoleLine>(AppShell.Header());

        lines.Add(Panel.Top("STATUS"));
        lines.Add(Panel.Blank());
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
            .. FieldRow.Build("VOICE", StatusLabelWidth, AppStateFormatting.OnOff(_appState.VoiceInputActive), StatusValueWidth),
            .. FieldRow.Build("WINDOW", StatusLabelWidth, AppStateFormatting.Window(_appState))
        ]));
        lines.Add(Panel.Blank());
        lines.Add(Panel.Bottom());
        lines.Add(ConsoleLine.Empty);

        lines.Add(Panel.Top("CONTROLS"));
        lines.Add(Panel.Blank());
        lines.Add(Panel.Row(
        [
            .. ControllerButton.Cell(ControllerButton.LeftStick, "Move Cursor", StickBadgeWidth, StickActionWidth),
            .. ControllerButton.Cell(ControllerButton.RightStick, "Scroll", StickBadgeWidth, StickActionWidth)
        ]));
        lines.Add(Panel.Blank());

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

        lines.Add(Panel.Blank());
        lines.Add(Panel.Bottom());
        lines.Add(ConsoleLine.Empty);

        lines.Add(Panel.Top("MENU"));
        lines.Add(Panel.Blank());

        for (var i = 0; i < _destinations.Count; i++)
        {
            lines.Add(MenuItemRow.Build(
                _destinations[i].Label,
                _destinations[i].Description,
                focused: i == SelectedIndex,
                MenuLabelWidth));
        }

        lines.Add(Panel.Blank());
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
