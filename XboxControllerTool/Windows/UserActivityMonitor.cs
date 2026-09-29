using System.Runtime.InteropServices;

namespace XboxControllerTool.Windows;

public interface IUserActivitySource
{
    /// <summary>How long it has been since Windows last saw any input from the user.</summary>
    TimeSpan IdleTime { get; }
}

/// <summary>
/// Reports how long the machine has been idle, straight from Windows' own idle clock - the same one
/// that decides when to blank the screen or sleep.
/// <para>
/// This covers the keyboard as well as the mouse, which is deliberate: someone typing is plainly
/// still there, and nudging their cursor mid-sentence would be worse than useless. Injected input
/// resets this clock too, which is exactly why a synthetic nudge keeps the machine awake at all.
/// </para>
/// </summary>
public sealed class UserActivityMonitor : IUserActivitySource
{
    public TimeSpan IdleTime
    {
        get
        {
            var info = new LastInputInfo { Size = (uint)Marshal.SizeOf<LastInputInfo>() };

            if (!GetLastInputInfo(ref info))
            {
                // Rather than guess, report no idle time at all so nothing is nudged on bad data.
                return TimeSpan.Zero;
            }

            // Both clocks wrap after about 49 days; unsigned subtraction stays correct across it.
            var elapsed = unchecked((uint)Environment.TickCount - info.TickCount);
            return TimeSpan.FromMilliseconds(elapsed);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint Size;
        public uint TickCount;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);
}
