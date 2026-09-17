using XboxControllerTool.Simulation;

namespace XboxControllerTool.Configuration;

public sealed class CustomButtonMapping
{
    /// <summary>
    /// Retained so settings written before HID buttons were supported still load; it is migrated
    /// into <see cref="ButtonId"/> and then left at zero.
    /// </summary>
    public ushort Button { get; set; }

    public string ButtonId { get; set; } = string.Empty;

    public KeyModifiers Modifiers { get; set; }

    public string Key { get; set; } = string.Empty;
}
