using XboxControllerTool.Core;

namespace XboxControllerTool.Input;

public readonly record struct ControllerSnapshot(ControllerState State, ControllerCapabilities Capabilities);
