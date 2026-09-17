namespace XboxControllerTool.Core;

public readonly record struct ControllerIdentity(int UserIndex, ControllerCapabilities Capabilities)
{
    public bool Matches(int userIndex, ControllerCapabilities capabilities) =>
        UserIndex == userIndex && Capabilities == capabilities;
}
