using System.Runtime.InteropServices;

namespace XboxControllerTool.Windows.RawInput;

[StructLayout(LayoutKind.Sequential)]
internal struct RawInputDevice
{
    public ushort UsagePage;
    public ushort Usage;
    public uint Flags;
    public nint Target;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RawInputDeviceListEntry
{
    public nint Device;
    public uint Type;
}

internal static class RawInputNative
{
    internal const int WmInput = 0x00FF;
    internal const uint WmClose = 0x0010;

    internal const ushort UsagePageGeneric = 0x01;
    internal const ushort UsagePageButton = 0x09;
    internal const ushort UsageJoystick = 0x04;
    internal const ushort UsageGamepad = 0x05;
    internal const ushort UsageMultiAxisController = 0x08;

    /// <summary>Delivers input even while this application is not in the foreground.</summary>
    internal const uint RawInputSink = 0x00000100;

    internal const uint RidInput = 0x10000003;
    internal const uint RidiPreparsedData = 0x20000005;
    internal const uint RidiDeviceName = 0x20000007;
    internal const uint RidiDeviceInfo = 0x2000000B;

    /// <summary>Byte offsets into RID_DEVICE_INFO for the HID branch of its union.</summary>
    internal const int DeviceInfoUsagePageOffset = 20;
    internal const int DeviceInfoUsageOffset = 22;

    internal const int RimTypeHid = 2;

    internal const int HidPInput = 0;
    internal const int HidPStatusSuccess = 0x00110000;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterRawInputDevices(RawInputDevice[] devices, uint deviceCount, uint size);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint GetRawInputData(nint rawInput, uint command, nint data, ref uint size, uint headerSize);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern uint GetRawInputDeviceInfo(nint device, uint command, nint data, ref uint size);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern uint GetRawInputDeviceList([Out] RawInputDeviceListEntry[]? list, ref uint count, uint entrySize);

    [DllImport("hid.dll")]
    internal static extern int HidP_MaxUsageListLength(int reportType, ushort usagePage, nint preparsedData);

    [DllImport("hid.dll")]
    internal static extern int HidP_GetUsages(
        int reportType,
        ushort usagePage,
        ushort linkCollection,
        [Out] ushort[] usageList,
        ref int usageLength,
        nint preparsedData,
        nint report,
        int reportLength);
}
