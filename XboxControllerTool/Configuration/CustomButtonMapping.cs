using XboxControllerTool.Simulation;

namespace XboxControllerTool.Configuration;

public sealed class CustomButtonMapping
{
    public ushort Button { get; set; }
    public KeyModifiers Modifiers { get; set; }
    public string Key { get; set; } = string.Empty;
}
