using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace XboxControllerTool.Windows.RawInput;

/// <summary>
/// Reports how many buttons each attached gamepad declares to Windows. That number is a hard
/// ceiling: a button the controller never declares cannot be detected by this application or any
/// other, because Windows is never told it exists.
/// </summary>
public static partial class RawGamepadInventory
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromSeconds(2);
    private static readonly Lock Gate = new();

    private static IReadOnlyList<RawGamepadDevice> _cached = [];
    private static DateTime _cachedUtc = DateTime.MinValue;

    public static IReadOnlyList<RawGamepadDevice> Describe()
    {
        lock (Gate)
        {
            if (DateTime.UtcNow - _cachedUtc < CacheLifetime)
            {
                return _cached;
            }

            _cached = Enumerate();
            _cachedUtc = DateTime.UtcNow;
            return _cached;
        }
    }

    private static IReadOnlyList<RawGamepadDevice> Enumerate()
    {
        try
        {
            return EnumerateCore();
        }
        catch (Exception ex) when (ex is SEHException or OutOfMemoryException)
        {
            return [];
        }
    }

    private static List<RawGamepadDevice> EnumerateCore()
    {
        var entrySize = (uint)Marshal.SizeOf<RawInputDeviceListEntry>();
        uint count = 0;

        if (RawInputNative.GetRawInputDeviceList(null, ref count, entrySize) == uint.MaxValue || count == 0)
        {
            return [];
        }

        var entries = new RawInputDeviceListEntry[count];

        if (RawInputNative.GetRawInputDeviceList(entries, ref count, entrySize) == uint.MaxValue)
        {
            return [];
        }

        var devices = new List<RawGamepadDevice>();

        foreach (var entry in entries)
        {
            if (entry.Type != RawInputNative.RimTypeHid)
            {
                continue;
            }

            if (!IsGamepad(entry.Device) || ReadName(entry.Device) is not { } name)
            {
                continue;
            }

            devices.Add(new RawGamepadDevice(IdentityOf(name), CountButtons(entry.Device)));
        }

        return devices;
    }

    private static bool IsGamepad(nint device)
    {
        uint size = 0;

        if (RawInputNative.GetRawInputDeviceInfo(device, RawInputNative.RidiDeviceInfo, nint.Zero, ref size) != 0 || size == 0)
        {
            return false;
        }

        var buffer = Marshal.AllocHGlobal((int)size);

        try
        {
            // RID_DEVICE_INFO requires its own size in the first field before it will be filled in.
            Marshal.WriteInt32(buffer, 0, (int)size);

            if (RawInputNative.GetRawInputDeviceInfo(device, RawInputNative.RidiDeviceInfo, buffer, ref size) == uint.MaxValue)
            {
                return false;
            }

            var usagePage = (ushort)Marshal.ReadInt16(buffer, RawInputNative.DeviceInfoUsagePageOffset);
            var usage = (ushort)Marshal.ReadInt16(buffer, RawInputNative.DeviceInfoUsageOffset);

            return usagePage == RawInputNative.UsagePageGeneric &&
                   usage is RawInputNative.UsageGamepad or RawInputNative.UsageJoystick or RawInputNative.UsageMultiAxisController;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static string? ReadName(nint device)
    {
        uint size = 0;

        if (RawInputNative.GetRawInputDeviceInfo(device, RawInputNative.RidiDeviceName, nint.Zero, ref size) != 0 || size == 0)
        {
            return null;
        }

        var buffer = Marshal.AllocHGlobal((int)size * sizeof(char));

        try
        {
            return RawInputNative.GetRawInputDeviceInfo(device, RawInputNative.RidiDeviceName, buffer, ref size) == uint.MaxValue
                ? null
                : Marshal.PtrToStringUni(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static int CountButtons(nint device)
    {
        uint size = 0;

        if (RawInputNative.GetRawInputDeviceInfo(device, RawInputNative.RidiPreparsedData, nint.Zero, ref size) != 0 || size == 0)
        {
            return 0;
        }

        var buffer = Marshal.AllocHGlobal((int)size);

        try
        {
            if (RawInputNative.GetRawInputDeviceInfo(device, RawInputNative.RidiPreparsedData, buffer, ref size) == uint.MaxValue)
            {
                return 0;
            }

            return Math.Max(RawInputNative.HidP_MaxUsageListLength(RawInputNative.HidPInput, RawInputNative.UsagePageButton, buffer), 0);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public static string IdentityOf(string devicePath)
    {
        var match = VendorProductPattern().Match(devicePath);

        return match.Success
            ? $"{match.Groups[1].Value.ToUpperInvariant()}-{match.Groups[2].Value.ToUpperInvariant()}"
            : "UNKNOWN";
    }

    [GeneratedRegex(@"VID_([0-9A-Fa-f]{4})&PID_([0-9A-Fa-f]{4})")]
    private static partial Regex VendorProductPattern();
}
