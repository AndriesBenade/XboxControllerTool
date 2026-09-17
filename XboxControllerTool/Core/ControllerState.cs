namespace XboxControllerTool.Core;

public readonly record struct ControllerState(
    int UserIndex,
    bool IsConnected,
    uint PacketNumber,
    GamepadButton Buttons,
    short LeftThumbX,
    short LeftThumbY,
    short RightThumbX,
    short RightThumbY,
    byte LeftTrigger,
    byte RightTrigger)
{
    public static ControllerState Disconnected(int userIndex) => new(userIndex, false, 0, GamepadButton.None, 0, 0, 0, 0, 0, 0);

    public bool IsPressed(GamepadButton button) => (Buttons & button) == button;
}
