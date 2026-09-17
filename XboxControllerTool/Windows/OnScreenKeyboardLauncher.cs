using System.ComponentModel;
using System.Diagnostics;

namespace XboxControllerTool.Windows;

public sealed class OnScreenKeyboardLauncher : IOnScreenKeyboardLauncher
{
    public bool TryOpen()
    {
        try
        {
            var systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);
            var oskPath = Path.Combine(systemDirectory, "osk.exe");

            if (!File.Exists(oskPath))
            {
                return false;
            }

            Process.Start(new ProcessStartInfo(oskPath) { UseShellExecute = true });
            return true;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }
}
