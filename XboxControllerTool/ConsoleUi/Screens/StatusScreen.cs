using XboxControllerTool.Application;
using XboxControllerTool.Configuration;

namespace XboxControllerTool.ConsoleUi.Screens;

public sealed class StatusScreen(AppState appState, AppSettings settings) : IScreen
{
    private const int LeftLabelWidth = 18;
    private const int LeftValueWidth = 20;
    private const int RightLabelWidth = 18;

    public IReadOnlyList<ConsoleLine> BuildLines()
    {
        var lines = new List<ConsoleLine>(AppShell.Header());

        lines.Add(Panel.Top("INPUT STATE"));
        lines.Add(Panel.Blank());
        lines.Add(Pair(
            "CONTROLLER", AppStateFormatting.Connection(appState),
            "MODE", AppStateFormatting.Mode(appState)));
        lines.Add(Pair(
            "SELECTED PAD", AppStateFormatting.SelectedPad(appState),
            "SPEED", AppStateFormatting.Speed(appState)));
        lines.Add(Pair(
            "VOICE INPUT", AppStateFormatting.OnOff(appState.VoiceInputActive),
            "WINDOW", AppStateFormatting.Window(appState)));
        lines.Add(Pair(
            "DESKTOP INPUT", AppStateFormatting.DesktopInput(appState),
            "THEME", Value(ConsoleTheme.Current.Name.ToUpperInvariant())));
        lines.Add(Panel.Blank());

        lines.Add(Panel.Section("SENSITIVITY"));
        lines.Add(Panel.Blank());
        lines.Add(Pair(
            "MOUSE", Value(settings.MouseSensitivity.ToString("0.0")),
            "SCROLL", Value(settings.ScrollSensitivity.ToString("0.0"))));
        lines.Add(Pair(
            "PRECISION", Value($"{settings.PrecisionMultiplier * 100:0}%"),
            "PRECISION SCROLL", Value($"{settings.ScrollPrecisionMultiplier * 100:0}%")));
        lines.Add(Pair(
            "ACCELERATION", AppStateFormatting.OnOff(settings.MouseAccelerationEnabled),
            "PAUSE IN GAMES", AppStateFormatting.OnOff(settings.PauseOnFocusedGame)));
        lines.Add(Pair(
            "STICK DEAD ZONE", Value($"{settings.StickDeadZone * 100:0}%"),
            "SCROLL DEAD ZONE", Value($"{settings.ScrollDeadZone * 100:0}%")));
        lines.Add(Panel.Blank());

        lines.Add(Panel.Section("ALERTS"));
        lines.Add(Panel.Blank());
        lines.Add(Pair(
            "DURATION", Value($"{settings.NotificationDurationMs / 1000.0:0.0}s"),
            "AUDIO", AppStateFormatting.OnOff(settings.AudioFeedbackEnabled)));
        lines.Add(Pair(
            "POSITION", Value(SettingsText.Position(settings.NotificationPosition)),
            string.Empty, Value(string.Empty)));
        lines.Add(Panel.Blank());
        lines.Add(Panel.Bottom());

        lines.AddRange(AppShell.Footer((ControllerButton.B, "Back")));

        return lines;
    }

    public NavigationCommand HandleAction(MenuAction action) =>
        action == MenuAction.Cancel ? NavigationCommand.Pop : NavigationCommand.None;

    private static ConsoleLine Pair(string leftLabel, ConsoleSegment leftValue, string rightLabel, ConsoleSegment rightValue) =>
        Panel.Row(
        [
            .. FieldRow.Build(leftLabel, LeftLabelWidth, leftValue, LeftValueWidth),
            .. FieldRow.Build(rightLabel, RightLabelWidth, rightValue)
        ]);

    private static ConsoleSegment Value(string text) => new(text, ConsoleTheme.Value);
}
