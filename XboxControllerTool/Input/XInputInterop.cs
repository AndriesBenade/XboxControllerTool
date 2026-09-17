using System.Runtime.InteropServices;

namespace XboxControllerTool.Input;

[StructLayout(LayoutKind.Sequential)]
internal struct XInputGamepad
{
    public ushort wButtons;
    public byte bLeftTrigger;
    public byte bRightTrigger;
    public short sThumbLX;
    public short sThumbLY;
    public short sThumbRX;
    public short sThumbRY;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XInputState
{
    public uint dwPacketNumber;
    public XInputGamepad Gamepad;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XInputVibration
{
    public ushort wLeftMotorSpeed;
    public ushort wRightMotorSpeed;
}

[StructLayout(LayoutKind.Sequential)]
internal struct XInputCapabilities
{
    public byte Type;
    public byte SubType;
    public ushort Flags;
    public XInputGamepad Gamepad;
    public XInputVibration Vibration;
}

internal static partial class XInputNative
{
    internal const int ErrorSuccess = 0;
    internal const int UserCount = 4;
    internal const uint XInputFlagGamepad = 0x00000001;

    [LibraryImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    internal static partial int XInputGetState(int dwUserIndex, out XInputState pState);

    [LibraryImport("xinput1_4.dll", EntryPoint = "XInputGetCapabilities")]
    internal static partial int XInputGetCapabilities(int dwUserIndex, uint dwFlags, out XInputCapabilities pCapabilities);
}
