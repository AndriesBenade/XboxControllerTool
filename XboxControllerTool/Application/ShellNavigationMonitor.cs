using XboxControllerTool.Windows;

namespace XboxControllerTool.Application;

public enum ShellFocusTransition
{
    None,
    ShellFocused,
    ShellUnfocused
}

/// <summary>
/// Tracks whether a Windows surface that navigates itself with a gamepad is in front. Windows offers
/// no way to stop the shell reading the controller, so the only way to avoid two things reacting to
/// one button press is for this application to stand back while such a surface is focused.
/// </summary>
public sealed class ShellNavigationMonitor
{
    private readonly IForegroundWindowSource _foregroundSource;
    private readonly IShellSurfaceClassifier _classifier;
    private readonly int _ownProcessId;

    public ShellNavigationMonitor(IForegroundWindowSource foregroundSource, IShellSurfaceClassifier classifier, int? ownProcessId = null)
    {
        _foregroundSource = foregroundSource;
        _classifier = classifier;
        _ownProcessId = ownProcessId ?? Environment.ProcessId;
    }

    public bool IsShellFocused { get; private set; }

    public string? SurfaceName { get; private set; }

    public ShellFocusTransition Update(bool detectionEnabled)
    {
        if (!detectionEnabled)
        {
            return Apply(false, null);
        }

        var foreground = _foregroundSource.GetForeground();

        if (foreground.ProcessId == _ownProcessId)
        {
            return Apply(false, null);
        }

        var surface = _classifier.Classify(foreground.ProcessId, foreground.Handle);
        return Apply(surface.IsGamepadNavigable, surface.Name);
    }

    private ShellFocusTransition Apply(bool isShellFocused, string? surfaceName)
    {
        SurfaceName = surfaceName;

        if (isShellFocused == IsShellFocused)
        {
            return ShellFocusTransition.None;
        }

        IsShellFocused = isShellFocused;
        return isShellFocused ? ShellFocusTransition.ShellFocused : ShellFocusTransition.ShellUnfocused;
    }
}
