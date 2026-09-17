using System.Runtime.InteropServices;

namespace XboxControllerTool.Simulation;

[StructLayout(LayoutKind.Sequential)]
internal struct MouseInput
{
    public int dx;
    public int dy;
    public uint mouseData;
    public uint dwFlags;
    public uint time;
    public nint dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct KeyboardInput
{
    public ushort wVk;
    public ushort wScan;
    public uint dwFlags;
    public uint time;
    public nint dwExtraInfo;
}

[StructLayout(LayoutKind.Explicit)]
internal struct InputUnion
{
    [FieldOffset(0)] public MouseInput Mouse;
    [FieldOffset(0)] public KeyboardInput Keyboard;
}

[StructLayout(LayoutKind.Sequential)]
internal struct Input
{
    public uint type;
    public InputUnion u;
}

internal static partial class NativeInput
{
    internal const uint InputMouse = 0;
    internal const uint InputKeyboard = 1;

    internal const uint MouseEventMove = 0x0001;
    internal const uint MouseEventLeftDown = 0x0002;
    internal const uint MouseEventLeftUp = 0x0004;
    internal const uint MouseEventRightDown = 0x0008;
    internal const uint MouseEventRightUp = 0x0010;
    internal const uint MouseEventWheel = 0x0800;

    internal const uint KeyEventKeyUp = 0x0002;

    internal const ushort VkLeftWindows = 0x5B;
    internal const ushort VkH = 0x48;
    internal const ushort VkMenu = 0x12;
    internal const ushort VkEscape = 0x1B;
    internal const ushort VkBack = 0x08;
    internal const ushort VkLeft = 0x25;
    internal const ushort VkRight = 0x27;
    internal const ushort VkD = 0x44;
    internal const ushort VkReturn = 0x0D;

    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial uint SendInput(uint nInputs, Input[] pInputs, int cbSize);
}
