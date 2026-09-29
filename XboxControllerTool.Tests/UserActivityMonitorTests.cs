using XboxControllerTool.Application;
using XboxControllerTool.Simulation;
using XboxControllerTool.Windows;

namespace XboxControllerTool.Tests;

/// <summary>
/// The whole feature rests on one assumption that cannot be reasoned into being true: that input
/// this application injects resets the same idle clock Windows uses to blank the screen. These run
/// it for real on the build machine.
/// </summary>
public class UserActivityMonitorTests
{
    [Fact]
    public void TheIdleClockIsReadable()
    {
        var idle = new UserActivityMonitor().IdleTime;

        Assert.True(idle >= TimeSpan.Zero, "Idle time went backwards.");
        Assert.True(idle < TimeSpan.FromDays(49), $"Idle time of {idle} means the tick wraparound is mishandled.");
    }

    [Fact]
    public void AnInjectedNudgeResetsWindowsOwnIdleClock()
    {
        var monitor = new UserActivityMonitor();
        var mouse = new MouseSimulator();

        var before = monitor.IdleTime;

        // The real circle, through the real injector. It returns the cursor to where it started.
        foreach (var (dx, dy) in CircularNudge.Path)
        {
            mouse.MoveRelative(dx, dy);
        }

        var after = monitor.IdleTime;

        Assert.True(after <= before, $"Idle time rose from {before} to {after} across a nudge.");
        Assert.True(
            after < TimeSpan.FromSeconds(1),
            $"Injected movement left the machine looking idle for {after}, so it would not keep the screen on.");
    }
}
