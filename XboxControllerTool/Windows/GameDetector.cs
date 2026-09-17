using System.ComponentModel;
using System.Diagnostics;

namespace XboxControllerTool.Windows;

public readonly record struct GameClassification(bool IsGame, string ProcessName, string Reason);

public interface IGameClassifier
{
    GameClassification Classify(int processId);
}

public sealed class GameDetector : IGameClassifier
{
    private static readonly string[] GameInstallMarkers =
    [
        @"\steamapps\common\",
        @"\epic games\",
        @"\xboxgames\",
        @"\gog galaxy\games\",
        @"\gog games\",
        @"\ubisoft game launcher\",
        @"\origin games\",
        @"\ea games\",
        @"\riot games\",
        @"\games\"
    ];

    private static readonly string[] ControllerRuntimeModules =
    [
        "xinput1_4.dll",
        "xinput1_3.dll",
        "xinput1_2.dll",
        "xinput1_1.dll",
        "xinput9_1_0.dll",
        "gameinput.dll",
        "dinput8.dll"
    ];

    public GameClassification Classify(int processId)
    {
        var executablePath = NativeProcessInfo.TryGetExecutablePath(processId);
        var processName = string.IsNullOrEmpty(executablePath)
            ? TryGetProcessName(processId)
            : Path.GetFileNameWithoutExtension(executablePath);

        if (string.IsNullOrEmpty(executablePath))
        {
            return new GameClassification(false, processName, "unknown process");
        }

        if (IsWindowsComponent(executablePath))
        {
            return new GameClassification(false, processName, "windows component");
        }

        var marker = MatchGameInstallMarker(executablePath);
        if (marker is not null)
        {
            return new GameClassification(true, processName, $"installed under {marker.Trim('\\')}");
        }

        var module = MatchControllerRuntimeModule(processId);
        if (module is not null)
        {
            return new GameClassification(true, processName, $"uses controller runtime ({module})");
        }

        return new GameClassification(false, processName, "no game signal");
    }

    private static string? MatchGameInstallMarker(string executablePath)
    {
        var normalized = executablePath.ToLowerInvariant();
        return GameInstallMarkers.FirstOrDefault(marker => normalized.Contains(marker, StringComparison.Ordinal));
    }

    private static string? MatchControllerRuntimeModule(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);

            foreach (ProcessModule module in process.Modules)
            {
                using (module)
                {
                    var name = module.ModuleName;

                    if (name is not null && ControllerRuntimeModules.Contains(name, StringComparer.OrdinalIgnoreCase))
                    {
                        return name;
                    }
                }
            }

            return null;
        }
        catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or ArgumentException or NotSupportedException)
        {
            return null;
        }
    }

    private static bool IsWindowsComponent(string executablePath)
    {
        var windowsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        return !string.IsNullOrEmpty(windowsDirectory)
            && executablePath.StartsWith(windowsDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static string TryGetProcessName(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            return process.ProcessName;
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return "unknown";
        }
    }
}
