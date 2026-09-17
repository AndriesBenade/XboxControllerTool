using XboxControllerTool.Application;
using XboxControllerTool.Windows;

namespace XboxControllerTool.Tests;

public class ShellNavigationMonitorTests
{
    private const int OwnProcessId = 4242;
    private const int ShellProcessId = 100;
    private const int OrdinaryProcessId = 200;

    [Fact]
    public void FocusingAGamepadNavigableSurfaceIsReportedOnce()
    {
        var monitor = Create(out var foreground, out _);

        foreground.Set(ShellProcessId);

        Assert.Equal(ShellFocusTransition.ShellFocused, monitor.Update(detectionEnabled: true));
        Assert.Equal(ShellFocusTransition.None, monitor.Update(detectionEnabled: true));
        Assert.True(monitor.IsShellFocused);
        Assert.Equal("Start Menu", monitor.SurfaceName);
    }

    [Fact]
    public void LeavingTheSurfaceIsReportedOnce()
    {
        var monitor = Create(out var foreground, out _);

        foreground.Set(ShellProcessId);
        monitor.Update(detectionEnabled: true);

        foreground.Set(OrdinaryProcessId);

        Assert.Equal(ShellFocusTransition.ShellUnfocused, monitor.Update(detectionEnabled: true));
        Assert.Equal(ShellFocusTransition.None, monitor.Update(detectionEnabled: true));
        Assert.False(monitor.IsShellFocused);
    }

    [Fact]
    public void AnOrdinaryWindowIsNeverTreatedAsAShellSurface()
    {
        var monitor = Create(out var foreground, out _);

        foreground.Set(OrdinaryProcessId);

        Assert.Equal(ShellFocusTransition.None, monitor.Update(detectionEnabled: true));
        Assert.False(monitor.IsShellFocused);
        Assert.Null(monitor.SurfaceName);
    }

    [Fact]
    public void TheApplicationsOwnWindowIsNeverAShellSurface()
    {
        var monitor = Create(out var foreground, out var classifier);

        foreground.Set(OwnProcessId);
        monitor.Update(detectionEnabled: true);

        Assert.False(monitor.IsShellFocused);
        Assert.Empty(classifier.Classified);
    }

    [Fact]
    public void TurningTheSettingOffReleasesAnActivePause()
    {
        var monitor = Create(out var foreground, out _);

        foreground.Set(ShellProcessId);
        monitor.Update(detectionEnabled: true);

        Assert.Equal(ShellFocusTransition.ShellUnfocused, monitor.Update(detectionEnabled: false));
        Assert.False(monitor.IsShellFocused);
    }

    [Fact]
    public void NothingIsClassifiedWhileTheSettingIsOff()
    {
        var monitor = Create(out var foreground, out var classifier);

        foreground.Set(ShellProcessId);
        monitor.Update(detectionEnabled: false);

        Assert.Empty(classifier.Classified);
        Assert.False(monitor.IsShellFocused);
    }

    private static ShellNavigationMonitor Create(out FakeForeground foreground, out FakeShellClassifier classifier)
    {
        foreground = new FakeForeground();
        classifier = new FakeShellClassifier(ShellProcessId, "Start Menu");
        return new ShellNavigationMonitor(foreground, classifier, OwnProcessId);
    }

    private sealed class FakeForeground : IForegroundWindowSource
    {
        private ForegroundWindowInfo _info;

        public void Set(int processId) => _info = new ForegroundWindowInfo(processId, processId);

        public ForegroundWindowInfo GetForeground() => _info;
    }

    private sealed class FakeShellClassifier(int navigableProcessId, string surfaceName) : IShellSurfaceClassifier
    {
        public List<int> Classified { get; } = [];

        public ShellSurface Classify(int processId, nint windowHandle)
        {
            Classified.Add(processId);

            return processId == navigableProcessId
                ? new ShellSurface(true, surfaceName)
                : new ShellSurface(false, null);
        }
    }
}
