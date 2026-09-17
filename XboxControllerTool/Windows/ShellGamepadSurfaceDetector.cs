using System.Runtime.InteropServices;
using System.Text;

namespace XboxControllerTool.Windows;

public readonly record struct ShellSurface(bool IsGamepadNavigable, string? Name);

public interface IShellSurfaceClassifier
{
    ShellSurface Classify(int processId, nint windowHandle);
}

/// <summary>
/// Recognises the Windows surfaces that drive themselves from a gamepad. Windows 11 navigates these
/// with the controller on its own, so while one of them is in front both it and this application
/// react to the same button press - the shell acting on whatever it has focused, this application
/// acting on whatever is under the cursor.
/// <para>
/// The list is deliberately narrow: only surfaces confirmed to navigate by gamepad are included, so
/// ordinary windows are never mistaken for one. Explorer hosts both the taskbar, which is
/// gamepad-navigable, and File Explorer windows, which are not, so it is matched by window class
/// rather than by process.
/// </para>
/// </summary>
public sealed class ShellGamepadSurfaceDetector : IShellSurfaceClassifier
{
    private static readonly Dictionary<string, string> NavigableProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        ["StartMenuExperienceHost"] = "Start Menu",
        ["ShellExperienceHost"] = "Windows Shell",
        ["SearchHost"] = "Search",
        ["SearchApp"] = "Search",
        ["SystemSettings"] = "Settings",
        ["ShellHost"] = "Windows Shell",
        ["LockApp"] = "Lock Screen"
    };

    /// <summary>
    /// The taskbar is deliberately absent. It navigates by gamepad like the other surfaces, but it
    /// takes focus far too easily - a click anywhere near the bottom of the screen is enough - and
    /// losing cursor control every time that happens is worse than the double-input it avoids.
    /// </summary>
    private static readonly Dictionary<string, string> NavigableWindowClasses = new(StringComparer.OrdinalIgnoreCase);

    public ShellSurface Classify(int processId, nint windowHandle)
    {
        if (windowHandle != nint.Zero && TryGetClassName(windowHandle) is { } className &&
            NavigableWindowClasses.TryGetValue(className, out var classSurface))
        {
            return new ShellSurface(true, classSurface);
        }

        if (processId == 0)
        {
            return new ShellSurface(false, null);
        }

        var executablePath = NativeProcessInfo.TryGetExecutablePath(processId);

        if (executablePath is null)
        {
            return new ShellSurface(false, null);
        }

        var processName = Path.GetFileNameWithoutExtension(executablePath);

        return NavigableProcesses.TryGetValue(processName, out var surface)
            ? new ShellSurface(true, surface)
            : new ShellSurface(false, null);
    }

    private static string? TryGetClassName(nint windowHandle)
    {
        var buffer = new StringBuilder(256);
        var length = GetClassName(windowHandle, buffer, buffer.Capacity);

        return length > 0 ? buffer.ToString(0, length) : null;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetClassNameW")]
    private static extern int GetClassName(nint window, StringBuilder className, int maxCount);
}
