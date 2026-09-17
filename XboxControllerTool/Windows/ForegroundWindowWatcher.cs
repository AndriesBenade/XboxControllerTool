using System.Runtime.InteropServices;

namespace XboxControllerTool.Windows;

public readonly record struct ForegroundWindowInfo(nint Handle, int ProcessId);

public interface IForegroundWindowSource
{
    ForegroundWindowInfo GetForeground();
}

public sealed class ForegroundWindowWatcher : IForegroundWindowSource
{
    public ForegroundWindowInfo GetForeground()
    {
        var handle = NativeWindowMethods.GetForegroundWindow();

        if (handle == nint.Zero)
        {
            return new ForegroundWindowInfo(handle, 0);
        }

        NativeWindowMethods.GetWindowThreadProcessId(handle, out var processId);
        return new ForegroundWindowInfo(handle, (int)processId);
    }
}

internal static class NativeProcessInfo
{
    private const uint QueryLimitedInformation = 0x1000;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "QueryFullProcessImageNameW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageName(nint process, uint flags, System.Text.StringBuilder exeName, ref int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint handle);

    internal static string? TryGetExecutablePath(int processId)
    {
        var handle = OpenProcess(QueryLimitedInformation, false, processId);

        if (handle == nint.Zero)
        {
            return null;
        }

        try
        {
            var capacity = 1024;
            var buffer = new System.Text.StringBuilder(capacity);

            return QueryFullProcessImageName(handle, 0, buffer, ref capacity)
                ? buffer.ToString()
                : null;
        }
        finally
        {
            CloseHandle(handle);
        }
    }
}
