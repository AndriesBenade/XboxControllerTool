using System.ComponentModel;
using System.Diagnostics;

namespace XboxControllerTool.Windows;

public sealed class StartupManager
{
    private const string TaskName = "XboxControllerTool";
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(10);

    public bool IsEnabled() => RunScheduler($"/Query /TN \"{TaskName}\"");

    public bool Enable()
    {
        var executablePath = Environment.ProcessPath;

        if (string.IsNullOrEmpty(executablePath))
        {
            return false;
        }

        var user = $"{Environment.UserDomainName}\\{Environment.UserName}";

        return RunScheduler(
            $"/Create /TN \"{TaskName}\" /TR \"\\\"{executablePath}\\\"\" /SC ONLOGON /RU \"{user}\" /IT /RL HIGHEST /F");
    }

    public bool Disable() => RunScheduler($"/Delete /TN \"{TaskName}\" /F");

    private static bool RunScheduler(string arguments)
    {
        var startInfo = new ProcessStartInfo("schtasks.exe", arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            using var process = Process.Start(startInfo);

            if (process is null)
            {
                return false;
            }

            process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();

            return process.WaitForExit(CommandTimeout) && process.ExitCode == 0;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or SystemException)
        {
            return false;
        }
    }
}
