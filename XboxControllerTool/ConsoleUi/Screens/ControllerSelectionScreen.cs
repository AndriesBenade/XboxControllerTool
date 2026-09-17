using XboxControllerTool.Application;
using XboxControllerTool.Input;

namespace XboxControllerTool.ConsoleUi.Screens;

public sealed class ControllerSelectionScreen : ListMenuScreen
{
    private const int MenuLabelWidth = 18;
    private const int FieldLabelWidth = 12;
    private const int SlotLabelWidth = 8;
    private const int SlotValueWidth = 28;

    private static readonly (string Label, string Description)[] Options =
    [
        ("ALL CONTROLLERS", "Any connected pad drives the desktop"),
        ("SPECIFIC PAD", "Only the pad you pick drives the desktop")
    ];

    private readonly ControllerSelectionService _selectionService;
    private readonly AppState _appState;

    public ControllerSelectionScreen(ControllerSelectionService selectionService, AppState appState)
    {
        _selectionService = selectionService;
        _appState = appState;
    }

    protected override int ItemCount => Options.Length;

    public override IReadOnlyList<ConsoleLine> BuildLines()
    {
        var lines = new List<ConsoleLine>(AppShell.Header());

        if (_selectionService.IsWaitingForSelection)
        {
            lines.Add(Panel.Top("SELECT CONTROLLER"));
            lines.Add(Panel.Blank());
            lines.Add(Panel.Row(new ConsoleSegment("WAITING FOR INPUT", ConsoleTheme.Active)));
            lines.Add(Panel.Blank());
            lines.Add(Panel.Row(
            [
                new ConsoleSegment("Press ", ConsoleTheme.Text),
                .. ControllerButton.Render(ControllerButton.Start),
                new ConsoleSegment(" on the controller you want to use", ConsoleTheme.Text)
            ]));
            lines.Add(Panel.Blank());
            lines.Add(Panel.Row(new ConsoleSegment("Every other pad is then ignored by the desktop.", ConsoleTheme.Label)));
            lines.Add(Panel.Blank());
            lines.Add(Panel.Bottom());

            lines.AddRange(AppShell.Footer((ControllerButton.B, "Cancel")));
            return lines;
        }

        lines.Add(Panel.Top("CONTROLLER MODE"));
        lines.Add(Panel.Blank());

        for (var i = 0; i < Options.Length; i++)
        {
            lines.Add(MenuItemRow.Build(
                Options[i].Label,
                Options[i].Description,
                focused: i == SelectedIndex,
                MenuLabelWidth));
        }

        lines.Add(Panel.Blank());
        lines.Add(Panel.Section("DETECTED PADS"));
        lines.Add(Panel.Blank());

        for (var slot = 0; slot < _appState.SlotConnected.Length; slot += 2)
        {
            lines.Add(Panel.Row(
            [
                .. FieldRow.Build($"PAD {slot + 1}", SlotLabelWidth, AppStateFormatting.Slot(_appState.SlotConnected[slot]), SlotValueWidth),
                .. FieldRow.Build($"PAD {slot + 2}", SlotLabelWidth, AppStateFormatting.Slot(_appState.SlotConnected[slot + 1]))
            ]));
        }

        lines.Add(Panel.Blank());
        lines.Add(Panel.Section("ACTIVE"));
        lines.Add(Panel.Blank());
        lines.Add(Panel.Row([.. FieldRow.Build("MODE", FieldLabelWidth, AppStateFormatting.Mode(_appState))]));
        lines.Add(Panel.Row([.. FieldRow.Build("SELECTED", FieldLabelWidth, AppStateFormatting.SelectedPad(_appState))]));
        lines.Add(Panel.Blank());
        lines.Add(Panel.Bottom());

        lines.AddRange(AppShell.Footer(
            (ControllerButton.A, "Select"),
            (ControllerButton.UpDown, "Navigate"),
            (ControllerButton.B, "Back")));

        return lines;
    }

    public override NavigationCommand HandleAction(MenuAction action)
    {
        if (!_selectionService.IsWaitingForSelection)
        {
            return base.HandleAction(action);
        }

        if (action == MenuAction.Cancel)
        {
            _selectionService.CancelSelection();
        }

        return NavigationCommand.None;
    }

    protected override NavigationCommand OnConfirm(int index)
    {
        if (index == 0)
        {
            _selectionService.ResetToAllControllers();
        }
        else
        {
            _selectionService.BeginSelectSpecificController();
        }

        return NavigationCommand.None;
    }
}
