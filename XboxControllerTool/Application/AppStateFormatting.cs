using XboxControllerTool.ConsoleUi;
using XboxControllerTool.Input;

namespace XboxControllerTool.Application;

public static class AppStateFormatting
{
    public static ConsoleSegment Connection(AppState state) => state.ConnectedControllerCount > 0
        ? new ConsoleSegment($"CONNECTED ({state.ConnectedControllerCount})", ConsoleTheme.Positive)
        : new ConsoleSegment("NO CONTROLLER", ConsoleTheme.Negative);

    public static ConsoleSegment Mode(AppState state) => state.ControllerMode == ControllerSelectionMode.AllControllers
        ? new ConsoleSegment("ALL CONTROLLERS", ConsoleTheme.Value)
        : new ConsoleSegment("SPECIFIC PAD", ConsoleTheme.Value);

    public static ConsoleSegment SelectedPad(AppState state)
    {
        if (state.ControllerMode == ControllerSelectionMode.AllControllers)
        {
            return new ConsoleSegment("ANY PAD", ConsoleTheme.Value);
        }

        var padNumber = (state.SelectedControllerUserIndex ?? 0) + 1;

        return state.SelectedControllerAvailability switch
        {
            SelectedControllerAvailability.Available => new ConsoleSegment($"PAD {padNumber}", ConsoleTheme.Positive),
            SelectedControllerAvailability.Unavailable => new ConsoleSegment($"PAD {padNumber} OFFLINE", ConsoleTheme.Negative),
            _ => new ConsoleSegment("NONE", ConsoleTheme.Value)
        };
    }

    public static ConsoleSegment Speed(AppState state)
    {
        if (state.PrecisionModeActive)
        {
            return new ConsoleSegment("PRECISION", ConsoleTheme.Focus);
        }

        return state.SpeedBoostActive
            ? new ConsoleSegment("FAST", ConsoleTheme.Active)
            : new ConsoleSegment("NORMAL", ConsoleTheme.Value);
    }

    public static ConsoleSegment OnOff(bool value, string onText = "ON", string offText = "OFF") => value
        ? new ConsoleSegment(onText, ConsoleTheme.Active)
        : new ConsoleSegment(offText, ConsoleTheme.Label);

    public static ConsoleSegment Window(AppState state) => state.ConsoleTopMost
        ? new ConsoleSegment("SHOWN", ConsoleTheme.Value)
        : new ConsoleSegment("HIDDEN", ConsoleTheme.Label);

    public static ConsoleSegment Slot(bool connected) => connected
        ? new ConsoleSegment("CONNECTED", ConsoleTheme.Positive)
        : new ConsoleSegment("--", ConsoleTheme.Label);
}
