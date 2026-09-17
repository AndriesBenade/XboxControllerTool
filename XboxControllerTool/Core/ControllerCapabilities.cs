namespace XboxControllerTool.Core;

public readonly record struct ControllerCapabilities(byte Type, byte SubType, ushort Flags)
{
    public static readonly ControllerCapabilities Unknown = new(0, 0, 0);
}
