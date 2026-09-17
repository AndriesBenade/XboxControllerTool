using XboxControllerTool.Input;

namespace XboxControllerTool.Configuration;

public sealed class AppSettings
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;

    public double MouseSensitivity { get; set; } = 14.0;
    public double ScrollSensitivity { get; set; } = 9.0;
    public double PrecisionMultiplier { get; set; } = 0.28;
    public double ScrollPrecisionMultiplier { get; set; } = 0.15;
    public double StickDeadZone { get; set; } = 0.20;
    public double ScrollDeadZone { get; set; } = 0.15;
    public bool MouseAccelerationEnabled { get; set; } = true;

    public int NotificationDurationMs { get; set; } = 2500;
    public NotificationPosition NotificationPosition { get; set; } = NotificationPosition.TopRight;
    public bool AudioFeedbackEnabled { get; set; } = true;

    public string ThemeName { get; set; } = "Dark";
    public ConsoleFontSize FontSize { get; set; } = ConsoleFontSize.Medium;

    public bool PauseOnFocusedGame { get; set; } = true;

    /// <summary>
    /// Stands back while a Windows surface that navigates itself with a gamepad is focused, so one
    /// button press is not acted on by both Windows and this application.
    /// </summary>
    public bool YieldToWindowsShell { get; set; } = true;

    public ControllerSelectionMode ControllerMode { get; set; } = ControllerSelectionMode.AllControllers;
    public int? SelectedControllerUserIndex { get; set; }
    public byte? SelectedControllerCapabilityType { get; set; }
    public byte? SelectedControllerCapabilitySubType { get; set; }
    public ushort? SelectedControllerCapabilityFlags { get; set; }

    /// <summary>Full path to a chosen browser, or null to use whatever Windows opens links with.</summary>
    public string? BrowserExecutablePath { get; set; }

    public List<CustomButtonMapping> CustomButtons { get; set; } = [];

    public WindowPlacementSettings Window { get; set; } = new();

    public static AppSettings CreateDefault() => new();

    public void ClampToValidRanges()
    {
        MouseSensitivity = Math.Clamp(MouseSensitivity, 1.0, 40.0);
        ScrollSensitivity = Math.Clamp(ScrollSensitivity, 1.0, 20.0);
        PrecisionMultiplier = Math.Clamp(PrecisionMultiplier, 0.05, 0.9);
        ScrollPrecisionMultiplier = Math.Clamp(ScrollPrecisionMultiplier, 0.05, 0.9);
        StickDeadZone = Math.Clamp(StickDeadZone, 0.02, 0.5);
        ScrollDeadZone = Math.Clamp(ScrollDeadZone, 0.02, 0.5);
        NotificationDurationMs = Math.Clamp(NotificationDurationMs, 800, 8000);

        if (string.IsNullOrWhiteSpace(ThemeName))
        {
            ThemeName = "Dark";
        }
    }
}
