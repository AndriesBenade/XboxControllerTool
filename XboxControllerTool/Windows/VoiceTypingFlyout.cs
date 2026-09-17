using System.Runtime.InteropServices;
using System.Text;

namespace XboxControllerTool.Windows;

public interface IVoiceTypingFlyout
{
    /// <summary>True when a Windows text-input panel is actually on screen.</summary>
    bool IsOpen { get; }

    /// <summary>
    /// Watches for the panel to go away after the Win+H toggle and closes it if it does not. Text
    /// already dictated has been inserted into the focused field and is never affected: this acts
    /// on the panel's own window rather than sending Escape to whatever has focus.
    /// </summary>
    void ForceCloseIfStillOpen();
}

/// <summary>
/// Closes Windows' voice typing panel when the Win+H toggle leaves it on screen.
/// <para>
/// Two things make this awkward. The panel's host window reports <c>IsWindowVisible</c> = true even
/// while nothing is shown, so visibility cannot be used to detect it; DWM cloaking is what actually
/// changes. And the window is UWP-hosted, so it may ignore a polite close. Dismissal is therefore
/// escalated - close, then Escape posted to that window, then hide - and every step is written to a
/// log, because this behaviour can only be observed on a machine with a microphone and a user.
/// </para>
/// </summary>
public sealed class VoiceTypingFlyout : IVoiceTypingFlyout
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(200);
    private const int PollAttempts = 12;

    private const int DwmaCloaked = 14;
    private const uint WmClose = 0x0010;
    private const uint WmSysCommand = 0x0112;
    private const uint WmKeyDown = 0x0100;
    private const uint WmKeyUp = 0x0101;
    private const nint ScClose = 0xF060;
    private const nint VkEscape = 0x1B;
    private const int SwHide = 0;

    private static readonly string[] InputHostProcesses =
    [
        "TextInputHost",
        "InputApp",
        "WindowsInternal.ComposableShell.Experiences.TextInput.InputApp",
        "ShellExperienceHost"
    ];

    private readonly VoiceTypingLog _log;

    public VoiceTypingFlyout(VoiceTypingLog? log = null)
    {
        _log = log ?? VoiceTypingLog.CreateDefault();
    }

    public bool IsOpen => FindPanels().Count > 0;

    public void ForceCloseIfStillOpen() => Task.Run(WatchAndClose);

    private void WatchAndClose()
    {
        try
        {
            _log.Begin();
            _log.Snapshot("everything on screen when voice typing was switched off", DescribeAllOnScreen());

            var dismissed = false;

            for (var attempt = 0; attempt < PollAttempts; attempt++)
            {
                Thread.Sleep(PollInterval);

                var panels = FindPanels();

                if (panels.Count == 0)
                {
                    _log.Line($"attempt {attempt}: no panel found among on-screen windows");
                    dismissed = true;
                    break;
                }

                foreach (var panel in panels)
                {
                    Dismiss(panel, attempt);
                }
            }

            if (!dismissed)
            {
                _log.Line("gave up: the panel was still on screen after every attempt");
            }

            // Finding nothing does not mean nothing is showing: the panel has been observed to stay
            // on screen while matching no visible, uncloaked, top-level window at all. The
            // unfiltered dump is the only way to find out where it actually lives.
            LogEverythingUnfiltered();
        }
        catch (Exception ex)
        {
            _log.Line($"failed: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void LogEverythingUnfiltered()
    {
        _log.Line("--- unfiltered survey ---");

        var inputHostProcesses = System.Diagnostics.Process.GetProcesses()
            .Where(process => InputHostProcesses.Any(host => process.ProcessName.Contains(host, StringComparison.OrdinalIgnoreCase)))
            .Select(process => $"{process.ProcessName} pid={process.Id}")
            .ToList();

        _log.Snapshot("input host processes running", inputHostProcesses);
        _log.Snapshot("every top level window, whatever its state", DescribeEveryTopLevelWindow());
        _log.Snapshot("windows owned by input host processes, with their children", DescribeInputHostTree());
    }

    /// <summary>
    /// Windows that exist only to service the input stack - one per process - and can never be the
    /// panel. Left in, they buried the survey: the log blew past its size cap and was truncated.
    /// </summary>
    private static readonly string[] PlumbingClasses =
    [
        "IME",
        "MSCTFIME UI",
        "CicLoaderWndClass",
        "CiceroUIWndFrame",
        ".NET-BroadcastEventWindow",
        "tooltips_class32"
    ];

    private static List<string> DescribeEveryTopLevelWindow()
    {
        var rows = new List<string>();

        EnumWindows((handle, _) =>
        {
            var className = ReadClassName(handle);

            if (PlumbingClasses.Any(plumbing => className.StartsWith(plumbing, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // A window with no area cannot be the panel either, whatever it claims about itself.
            if (!GetWindowRect(handle, out var rect) || rect.Right <= rect.Left || rect.Bottom <= rect.Top)
            {
                return true;
            }

            rows.Add(Describe(handle));
            return true;
        }, nint.Zero);

        return rows;
    }

    private static List<string> DescribeInputHostTree()
    {
        var rows = new List<string>();

        EnumWindows((handle, _) =>
        {
            if (!IsInputHost(handle))
            {
                return true;
            }

            rows.Add("HOST " + Describe(handle));

            EnumChildWindows(handle, (child, _) =>
            {
                rows.Add("     child " + Describe(child));
                return true;
            }, nint.Zero);

            return true;
        }, nint.Zero);

        return rows;
    }

    /// <summary>
    /// Escalates because a UWP-hosted window may ignore the polite options. Hiding is last: it makes
    /// the panel go away for certain, but Windows still believes it is open.
    /// </summary>
    private void Dismiss(nint panel, int attempt)
    {
        var description = Describe(panel);

        switch (attempt)
        {
            case < 3:
                _log.Line($"attempt {attempt}: WM_CLOSE + SC_CLOSE -> {description}");
                PostMessage(panel, WmSysCommand, ScClose, nint.Zero);
                PostMessage(panel, WmClose, nint.Zero, nint.Zero);
                break;

            case < 6:
                _log.Line($"attempt {attempt}: Escape posted to the panel -> {description}");
                PostMessage(panel, WmKeyDown, VkEscape, nint.Zero);
                PostMessage(panel, WmKeyUp, VkEscape, nint.Zero);
                break;

            default:
                _log.Line($"attempt {attempt}: hiding the window -> {description}");
                ShowWindow(panel, SwHide);
                break;
        }
    }

    private static List<nint> FindPanels()
    {
        var panels = new List<nint>();

        EnumWindows((handle, _) =>
        {
            if (IsOnScreen(handle) && IsInputHost(handle))
            {
                panels.Add(handle);
            }

            return true;
        }, nint.Zero);

        return panels;
    }

    private static List<string> DescribeAllOnScreen()
    {
        var rows = new List<string>();

        EnumWindows((handle, _) =>
        {
            if (IsOnScreen(handle))
            {
                rows.Add(Describe(handle));
            }

            return true;
        }, nint.Zero);

        return rows;
    }

    private static bool IsOnScreen(nint handle)
    {
        if (!IsWindowVisible(handle))
        {
            return false;
        }

        // A cloaked window is not actually shown, which is the state the panel's host sits in
        // whenever nothing is up. Visibility alone would report it as on screen forever.
        if (DwmGetWindowAttribute(handle, DwmaCloaked, out var cloaked, sizeof(int)) != 0 || cloaked != 0)
        {
            return false;
        }

        return GetWindowRect(handle, out var rect) && rect.Right > rect.Left && rect.Bottom > rect.Top;
    }

    private static bool IsInputHost(nint handle)
    {
        GetWindowThreadProcessId(handle, out var processId);

        if (processId == 0)
        {
            return false;
        }

        var executablePath = NativeProcessInfo.TryGetExecutablePath((int)processId);

        if (executablePath is null)
        {
            return false;
        }

        var processName = Path.GetFileNameWithoutExtension(executablePath);

        return InputHostProcesses.Any(host => processName.Contains(host, StringComparison.OrdinalIgnoreCase));
    }

    private static string Describe(nint handle)
    {
        GetWindowThreadProcessId(handle, out var processId);
        var executablePath = processId == 0 ? null : NativeProcessInfo.TryGetExecutablePath((int)processId);
        var processName = executablePath is null ? $"pid{processId}" : Path.GetFileNameWithoutExtension(executablePath);

        DwmGetWindowAttribute(handle, DwmaCloaked, out var cloaked, sizeof(int));
        GetWindowRect(handle, out var rect);

        return $"{processName} class={ReadClassName(handle)} title='{ReadWindowText(handle)}' " +
               $"cloaked={cloaked} rect={rect.Left},{rect.Top} {rect.Right - rect.Left}x{rect.Bottom - rect.Top} hwnd={handle}";
    }

    private static string ReadClassName(nint handle)
    {
        var buffer = new StringBuilder(256);
        var length = GetClassName(handle, buffer, buffer.Capacity);
        return length > 0 ? buffer.ToString(0, length) : string.Empty;
    }

    private static string ReadWindowText(nint handle)
    {
        var buffer = new StringBuilder(256);
        var length = GetWindowText(handle, buffer, buffer.Capacity);
        return length > 0 ? buffer.ToString(0, length) : string.Empty;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private delegate bool EnumWindowsProc(nint handle, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumWindows(EnumWindowsProc callback, nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumChildWindows(nint parent, EnumWindowsProc callback, nint parameter);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetClassNameW")]
    private static extern int GetClassName(nint window, StringBuilder className, int maxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetWindowTextW")]
    private static extern int GetWindowText(nint window, StringBuilder text, int maxCount);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out Rect rect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint window, int command);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(nint window, int attribute, out int value, int size);
}
