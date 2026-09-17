using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Win32;
using XboxControllerTool.Simulation;

namespace XboxControllerTool.Windows;

public enum BrowserActivationResult
{
    Opened,
    Focused,
    NavigatedForward
}

public sealed class DefaultBrowserController
{
    private readonly IKeyboardInput _keyboard;
    private bool _resolved;
    private string? _cachedExecutablePath;

    public DefaultBrowserController(IKeyboardInput keyboard)
    {
        _keyboard = keyboard;
    }

    public BrowserActivationResult ActivateOrNavigateForward()
    {
        var executablePath = ResolveDefaultBrowserExecutablePath();
        var processName = executablePath is null ? null : Path.GetFileNameWithoutExtension(executablePath);
        var windowHandles = processName is null ? [] : FindMainWindows(processName);

        if (windowHandles.Count == 0)
        {
            LaunchAndFocusInBackground(executablePath);
            return BrowserActivationResult.Opened;
        }

        if (windowHandles.Contains(NativeWindowMethods.GetForegroundWindow()))
        {
            _keyboard.SendAltRight();
            return BrowserActivationResult.NavigatedForward;
        }

        ForegroundWindowActivator.Activate(windowHandles[0]);
        return BrowserActivationResult.Focused;
    }

    private void LaunchAndFocusInBackground(string? executablePath)
    {
        Task.Run(() =>
        {
            try
            {
                var startInfo = executablePath is not null
                    ? new ProcessStartInfo(executablePath) { UseShellExecute = true }
                    : new ProcessStartInfo("http://") { UseShellExecute = true };

                Process.Start(startInfo);
            }
            catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or FileNotFoundException)
            {
                return;
            }

            var processName = executablePath is null ? null : Path.GetFileNameWithoutExtension(executablePath);
            if (processName is null)
            {
                return;
            }

            for (var attempt = 0; attempt < 30; attempt++)
            {
                Thread.Sleep(100);

                var windowHandles = FindMainWindows(processName);
                if (windowHandles.Count > 0)
                {
                    ForegroundWindowActivator.Activate(windowHandles[0]);
                    return;
                }
            }
        });
    }

    private static List<nint> FindMainWindows(string processName)
    {
        var handles = new List<nint>();

        foreach (var process in Process.GetProcessesByName(processName))
        {
            using (process)
            {
                if (process.MainWindowHandle != nint.Zero)
                {
                    handles.Add(process.MainWindowHandle);
                }
            }
        }

        return handles;
    }

    private string? ResolveDefaultBrowserExecutablePath()
    {
        if (_resolved)
        {
            return _cachedExecutablePath;
        }

        _resolved = true;
        _cachedExecutablePath = TryResolveFromRegistry();
        return _cachedExecutablePath;
    }

    private static string? TryResolveFromRegistry()
    {
        try
        {
            using var userChoiceKey = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice");

            if (userChoiceKey?.GetValue("ProgId") is not string progId || string.IsNullOrEmpty(progId))
            {
                return null;
            }

            using var commandKey = Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command");

            return commandKey?.GetValue(null) is string command && !string.IsNullOrEmpty(command)
                ? ExtractExecutablePath(command)
                : null;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            return null;
        }
    }

    private static string? ExtractExecutablePath(string command)
    {
        var trimmed = command.Trim();

        if (trimmed.StartsWith('"'))
        {
            var closingQuoteIndex = trimmed.IndexOf('"', 1);
            return closingQuoteIndex > 0 ? trimmed[1..closingQuoteIndex] : null;
        }

        var spaceIndex = trimmed.IndexOf(' ');
        return spaceIndex > 0 ? trimmed[..spaceIndex] : trimmed;
    }
}
