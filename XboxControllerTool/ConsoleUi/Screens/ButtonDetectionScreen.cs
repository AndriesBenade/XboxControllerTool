using XboxControllerTool.Application;

namespace XboxControllerTool.ConsoleUi.Screens;

/// <summary>
/// Confirms that a button actually reaches this machine before it can be mapped. Nothing is listed
/// from a table of buttons a controller might have: the user presses the button and it appears
/// only if Windows really reported it.
/// </summary>
public sealed class ButtonDetectionScreen : ListMenuScreen
{
    private const int LabelWidth = 16;

    private readonly CustomButtonService _customButtons;

    private int _baselineSequence;
    private ExtraButton? _confirmed;

    public ButtonDetectionScreen(CustomButtonService customButtons)
    {
        _customButtons = customButtons;
    }

    protected override int ItemCount => _confirmed is null ? 0 : 1;

    public override void OnEnter()
    {
        base.OnEnter();
        _baselineSequence = _customButtons.DetectionSequence;
        _confirmed = null;
    }

    public override IReadOnlyList<ConsoleLine> BuildLines()
    {
        CaptureDetection();

        var lines = new List<ConsoleLine>(AppShell.Header());

        lines.Add(Panel.Top("DETECT A BUTTON"));
        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Row(new ConsoleSegment("Press and release the button you want to map.", ConsoleTheme.Text)));
        lines.Add(Panel.Row(new ConsoleSegment("It can only be mapped once it shows up as detected below.", ConsoleTheme.Label)));
        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Section("RESULT"));
        LayoutMetrics.PanelPad(lines);

        if (_confirmed is { } button)
        {
            lines.Add(Panel.Row(
            [
                new ConsoleSegment("DETECTED".PadRight(LabelWidth), ConsoleTheme.Label),
                .. ControllerButton.Render(button.Label)
            ]));
            lines.Add(Panel.Row(
            [
                new ConsoleSegment("CURRENT".PadRight(LabelWidth), ConsoleTheme.Label),
                new ConsoleSegment(_customButtons.DescribeMapping(button.Id), ConsoleTheme.Value)
            ]));
            LayoutMetrics.PanelPad(lines);
            lines.Add(MenuItemRow.BuildSetting(
                "Map Button",
                SelectedIndex == 0,
                SettingControl.Action("Choose a key combination"),
                LabelWidth,
                ControllerButton.A));
        }
        else
        {
            lines.Add(Panel.Row(new ConsoleSegment("Waiting for a button press...", ConsoleTheme.Focus)));
            lines.Add(Panel.Row(new ConsoleSegment("Buttons used by built-in actions are ignored here.", ConsoleTheme.Label)));
        }

        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Section("WHAT WINDOWS CAN SEE"));
        LayoutMetrics.PanelPad(lines);

        var devices = _customButtons.Devices;

        if (devices.Count == 0)
        {
            lines.Add(Panel.Row(new ConsoleSegment("No gamepad is reporting to Windows right now.", ConsoleTheme.Label)));
        }

        foreach (var device in devices)
        {
            lines.Add(Panel.Row(
            [
                new ConsoleSegment(device.DeviceId.PadRight(LabelWidth), ConsoleTheme.Label),
                new ConsoleSegment($"declares {device.ButtonCount} buttons to Windows", ConsoleTheme.Value)
            ]));
        }

        LayoutMetrics.PanelPad(lines);

        foreach (var note in Notes())
        {
            lines.Add(Panel.Row(new ConsoleSegment(note, ConsoleTheme.Label)));
        }

        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Bottom());

        lines.AddRange(AppShell.Footer(
            (ControllerButton.A, "Map"),
            (ControllerButton.B, "Back")));

        return lines;
    }

    private IEnumerable<string> Notes()
    {
        if (_customButtons.RawDetectionAvailable)
        {
            yield return "A pad that declares 16 buttons has no spare ones to offer: M1, M2,";
            yield return "MX, AGL and AGR are handled inside the controller and never reach";
            yield return "Windows. Give them an output in the pad's own configuration app.";
        }
        else
        {
            yield return "Raw HID monitoring could not start, so only the buttons XInput";
            yield return "reports can be detected. Extra buttons such as M1, M2 or the";
            yield return "paddles will not appear.";
        }
    }

    private void CaptureDetection()
    {
        if (_customButtons.DetectionSequence == _baselineSequence)
        {
            return;
        }

        _baselineSequence = _customButtons.DetectionSequence;

        if (_customButtons.LastDetected is { } detected)
        {
            _confirmed = detected;
        }
    }

    protected override NavigationCommand OnConfirm(int index) =>
        _confirmed is { } button && index == 0
            ? NavigationCommand.Push(new ButtonMappingScreen(_customButtons, button.Id))
            : NavigationCommand.None;
}
