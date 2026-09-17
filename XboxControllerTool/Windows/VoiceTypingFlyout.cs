using System.Runtime.InteropServices;
using System.Text;

namespace XboxControllerTool.Windows;

public interface IVoiceTypingFlyout
{
    /// <summary>True when Windows' voice typing panel is actually on screen.</summary>
    bool IsOpen { get; }

    /// <summary>
    /// Watches for the panel to go away after the Win+H toggle and closes it directly if it does
    /// not. Text already dictated has been inserted into the focused field and is never affected:
    /// this closes the panel's own window rather than sending Escape to whatever has focus.
    /// </summary>
    void ForceCloseIfStillOpen();
}

/// <summary>
/// Finds and closes Windows' voice typing panel.
/// <para>
/// The panel lives in TextInputHost.exe as a <c>Windows.UI.Core.CoreWindow</c>. That window is
/// created once and reused, and it stays <c>IsWindowVisible</c> = true even while nothing is on
/// screen, so visibility is useless for deciding whether the panel is up. What actually changes is
/// DWM cloaking: while hidden the window reports <c>DWMWA_CLOAKED</c> = 2 (cloaked by the shell),
/// and while shown it reports 0. Cloaking is therefore what this checks.
/// </para>
/// </summary>
public sealed class VoiceTypingFlyout : IVoiceTypingFlyout
{
    private const string HostProcessName = "TextInputHost";
    private const string CoreWindowClass = "Windows.UI.Core.CoreWindow";
    private const int DwmaCloaked = 14;
    private const uint WmClose = 0x0010;
    private const uint WmSysCommand = 0x0112;
    private const nint ScClose = 0xF060;

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(150);
    private const int PollAttempts = 10;

    public bool IsOpen => FindOpenPanel() != nint.Zero;

    /// <summary>
    /// Runs in the background: the input loop ticks every few milliseconds and must never wait on
    /// the shell taking its time to dismiss a panel.
    /// </summary>
    public void ForceCloseIfStillOpen() => Task.Run(WatchAndClose);

    private async Task WatchAndClose()
    {
        for (var attempt = 0; attempt < PollAttempts; attempt++)
        {
            await Task.Delay(PollInterval);

            var panel = FindOpenPanel();

            if (panel == nint.Zero)
            {
                return;
            }

            // Asking the panel's own window to close leaves the dictated text alone, unlike sending
            // Escape to whatever happens to have focus, which could dismiss the user's own dialog.
            PostMessage(panel, WmSysCommand, ScClose, nint.Zero);
            PostMessage(panel, WmClose, nint.Zero, nint.Zero);
        }
    }

    private static nint FindOpenPanel()
    {
        var found = nint.Zero;

        EnumWindows((handle, _) =>
        {
            if (!IsVoiceTypingPanel(handle))
            {
                return true;
            }

            found = handle;
            return false;
        }, nint.Zero);

        return found;
    }

    private static bool IsVoiceTypingPanel(nint handle)
    {
        if (!IsWindowVisible(handle) || ReadClassName(handle) != CoreWindowClass)
        {
            return false;
        }

        GetWindowThreadProcessId(handle, out var processId);

        if (processId == 0)
        {
            return false;
        }

        var executablePath = NativeProcessInfo.TryGetExecutablePath((int)processId);

        if (executablePath is null ||
            !string.Equals(Path.GetFileNameWithoutExtension(executablePath), HostProcessName, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return IsUncloaked(handle);
    }

    private static bool IsUncloaked(nint handle) =>
        DwmGetWindowAttribute(handle, DwmaCloaked, out var cloaked, sizeof(int)) == 0 && cloaked == 0;

    private static string? ReadClassName(nint handle)
    {
        var buffer = new StringBuilder(256);
        var length = GetClassName(handle, buffer, buffer.Capacity);

        return length > 0 ? buffer.ToString(0, length) : null;
    }

    private delegate bool EnumWindowsProc(nint handle, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc callback, nint parameter);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetClassNameW")]
    private static extern int GetClassName(nint window, StringBuilder className, int maxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);
}
