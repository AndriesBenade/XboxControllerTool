using XboxControllerTool.Application;
using XboxControllerTool.Simulation;

namespace XboxControllerTool.ConsoleUi.Screens;

public sealed class ButtonMappingScreen : ListMenuScreen
{
    private const int LabelWidth = 16;

    private readonly CustomButtonService _customButtons;
    private readonly ushort _button;

    private KeyModifiers _modifiers;
    private string _group;
    private int _keyIndex;

    public ButtonMappingScreen(CustomButtonService customButtons, ushort button)
    {
        _customButtons = customButtons;
        _button = button;

        var existing = _customButtons.FindMapping(button);
        _modifiers = existing?.Modifiers ?? KeyModifiers.None;

        var key = existing is null ? null : KeyCatalog.Find(existing.Key);
        _group = key?.Group ?? KeyCatalog.Letters;
        _keyIndex = key is null ? 0 : Math.Max(KeyCatalog.InGroup(_group).ToList().FindIndex(k => k.Name == key.Value.Name), 0);
    }

    protected override int ItemCount => 7;

    private AssignableKey SelectedKey
    {
        get
        {
            var keys = KeyCatalog.InGroup(_group);
            return keys[Math.Clamp(_keyIndex, 0, keys.Count - 1)];
        }
    }

    public override IReadOnlyList<ConsoleLine> BuildLines()
    {
        var lines = new List<ConsoleLine>(AppShell.Header());

        lines.Add(Panel.Top($"MAP BUTTON {CustomButtonService.LabelFor(_button)}"));
        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Row(
        [
            new ConsoleSegment("SAVED".PadRight(LabelWidth), ConsoleTheme.Label),
            new ConsoleSegment(_customButtons.DescribeMapping(_button), ConsoleTheme.Value)
        ]));
        lines.Add(Panel.Row(
        [
            new ConsoleSegment("EDITING".PadRight(LabelWidth), ConsoleTheme.Label),
            new ConsoleSegment(KeyModifiersFormatting.Describe(_modifiers, SelectedKey.Name), ConsoleTheme.Focus)
        ]));
        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Section("COMBINATION"));
        LayoutMetrics.PanelPad(lines);

        lines.Add(Row(0, "Ctrl", SettingControl.Toggle(_modifiers.HasFlag(KeyModifiers.Control)), ControllerButton.A));
        lines.Add(Row(1, "Alt", SettingControl.Toggle(_modifiers.HasFlag(KeyModifiers.Alt)), ControllerButton.A));
        lines.Add(Row(2, "Shift", SettingControl.Toggle(_modifiers.HasFlag(KeyModifiers.Shift)), ControllerButton.A));
        lines.Add(Row(3, "Win", SettingControl.Toggle(_modifiers.HasFlag(KeyModifiers.Windows)), ControllerButton.A));
        lines.Add(Row(4, "Key Group", SettingControl.Cycle(_group), ControllerButton.LeftRight));
        lines.Add(Row(5, "Key", SettingControl.Cycle(SelectedKey.Name.ToUpperInvariant()), ControllerButton.LeftRight));

        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Section("ACTIONS"));
        LayoutMetrics.PanelPad(lines);
        lines.Add(Row(6, "Save Mapping", SettingControl.Action("Apply to this button"), ControllerButton.A));
        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Bottom());

        lines.AddRange(AppShell.Footer(
            (ControllerButton.A, "Select"),
            (ControllerButton.LeftRight, "Change"),
            (ControllerButton.UpDown, "Navigate"),
            (ControllerButton.B, "Back")));

        return lines;
    }

    protected override NavigationCommand OnConfirm(int index)
    {
        switch (index)
        {
            case 0:
                Toggle(KeyModifiers.Control);
                break;
            case 1:
                Toggle(KeyModifiers.Alt);
                break;
            case 2:
                Toggle(KeyModifiers.Shift);
                break;
            case 3:
                Toggle(KeyModifiers.Windows);
                break;
            case 6:
                _customButtons.Assign(_button, _modifiers, SelectedKey.Name);
                return NavigationCommand.Pop;
        }

        return NavigationCommand.None;
    }

    protected override NavigationCommand OnAdjust(int index, int direction)
    {
        switch (index)
        {
            case 4:
                CycleGroup(direction);
                break;
            case 5:
                CycleKey(direction);
                break;
        }

        return NavigationCommand.None;
    }

    private void Toggle(KeyModifiers modifier) => _modifiers ^= modifier;

    private void CycleGroup(int direction)
    {
        var groups = KeyCatalog.Groups;
        var index = groups.ToList().IndexOf(_group);
        _group = groups[(index + direction + groups.Count) % groups.Count];
        _keyIndex = 0;
    }

    private void CycleKey(int direction)
    {
        var keys = KeyCatalog.InGroup(_group);
        _keyIndex = (_keyIndex + direction + keys.Count) % keys.Count;
    }

    private ConsoleLine Row(int index, string label, ConsoleSegment[] control, string badge) =>
        MenuItemRow.BuildSetting(label, SelectedIndex == index, control, LabelWidth, badge);
}
