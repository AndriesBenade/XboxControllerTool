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

internal static unsafe partial class XInputNative
{
    internal const int ErrorSuccess = 0;
    internal const int UserCount = 4;
    internal const uint XInputFlagGamepad = 0x00000001;
    private const int GetStateExOrdinal = 100;

    // XInput's public XInputGetState masks the Guide button out. The ordinal-100 export returns the
    // unmasked state, which is the only way to observe Guide. It is undocumented, so it is resolved
    // dynamically and the public entry point is used whenever it cannot be loaded.
    private static readonly delegate* unmanaged[Stdcall]<int, XInputState*, int> GetStateEx = ResolveGetStateEx();

    internal static bool ExtendedStateAvailable => GetStateEx is not null;

    internal static int GetState(int userIndex, out XInputState state)
    {
        if (GetStateEx is null)
        {
            return XInputGetState(userIndex, out state);
        }

        XInputState local = default;
        var result = GetStateEx(userIndex, &local);
        state = local;
        return result;
    }

    [LibraryImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    internal static partial int XInputGetState(int dwUserIndex, out XInputState pState);

    [LibraryImport("xinput1_4.dll", EntryPoint = "XInputGetCapabilities")]
    internal static partial int XInputGetCapabilities(int dwUserIndex, uint dwFlags, out XInputCapabilities pCapabilities);

    [LibraryImport("kernel32.dll", EntryPoint = "LoadLibraryW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint LoadLibrary(string name);

    [LibraryImport("kernel32.dll", EntryPoint = "GetProcAddress")]
    private static partial nint GetProcAddressByOrdinal(nint module, nint ordinal);

    private static delegate* unmanaged[Stdcall]<int, XInputState*, int> ResolveGetStateEx()
    {
        try
        {
            var module = LoadLibrary("xinput1_4.dll");

            if (module == nint.Zero)
            {
                return null;
            }

            var address = GetProcAddressByOrdinal(module, GetStateExOrdinal);

            return address == nint.Zero
                ? null
                : (delegate* unmanaged[Stdcall]<int, XInputState*, int>)address;
        }
        catch (DllNotFoundException)
        {
            return null;
        }
        catch (EntryPointNotFoundException)
        {
            return null;
        }
    }
}
