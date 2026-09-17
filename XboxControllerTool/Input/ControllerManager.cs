using XboxControllerTool.Core;

namespace XboxControllerTool.Input;

public sealed class ControllerManager
{
    private readonly bool[] _wasConnected = new bool[XInputNative.UserCount];
    private readonly ControllerCapabilities[] _capabilities = new ControllerCapabilities[XInputNative.UserCount];

    public ControllerSnapshot[] Poll()
    {
        var snapshots = new ControllerSnapshot[XInputNative.UserCount];

        for (var userIndex = 0; userIndex < XInputNative.UserCount; userIndex++)
        {
            var result = XInputNative.GetState(userIndex, out var state);
            var isConnected = result == XInputNative.ErrorSuccess;

            if (isConnected != _wasConnected[userIndex])
            {
                _wasConnected[userIndex] = isConnected;
                _capabilities[userIndex] = isConnected ? QueryCapabilities(userIndex) : ControllerCapabilities.Unknown;
            }

            var controllerState = isConnected
                ? new ControllerState(
                    userIndex,
                    true,
                    state.dwPacketNumber,
                    (GamepadButton)state.Gamepad.wButtons,
                    state.Gamepad.sThumbLX,
                    state.Gamepad.sThumbLY,
                    state.Gamepad.sThumbRX,
                    state.Gamepad.sThumbRY,
                    state.Gamepad.bLeftTrigger,
                    state.Gamepad.bRightTrigger)
                : ControllerState.Disconnected(userIndex);

            snapshots[userIndex] = new ControllerSnapshot(controllerState, _capabilities[userIndex]);
        }

        return snapshots;
    }

    private static ControllerCapabilities QueryCapabilities(int userIndex)
    {
        var result = XInputNative.XInputGetCapabilities(userIndex, XInputNative.XInputFlagGamepad, out var capabilities);
        return result == XInputNative.ErrorSuccess
            ? new ControllerCapabilities(capabilities.Type, capabilities.SubType, capabilities.Flags)
            : ControllerCapabilities.Unknown;
    }
}
