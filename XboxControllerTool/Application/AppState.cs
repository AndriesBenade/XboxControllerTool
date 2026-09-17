using XboxControllerTool.Input;

namespace XboxControllerTool.Application;

public sealed class AppState
{
    public bool[] SlotConnected { get; } = new bool[4];

    public int ConnectedControllerCount { get; set; }
    public ControllerSelectionMode ControllerMode { get; set; }
    public SelectedControllerAvailability SelectedControllerAvailability { get; set; }
    public int? SelectedControllerUserIndex { get; set; }
    public bool PrecisionModeActive { get; set; }
    public bool VoiceInputActive { get; set; }
    public bool ConsoleTopMost { get; set; }
    public bool DesktopInputPaused { get; set; }
    public string? FocusedGameName { get; set; }
}
