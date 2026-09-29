using XboxControllerTool.Application;
using XboxControllerTool.Configuration;
using XboxControllerTool.Simulation;
using XboxControllerTool.Windows;

namespace XboxControllerTool.Tests;

public class KeepAwakeServiceTests
{
    private static readonly DateTime Start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void TheCircleReturnsTheCursorExactlyWhereItStarted()
    {
        var x = CircularNudge.Path.Sum(step => step.Dx);
        var y = CircularNudge.Path.Sum(step => step.Dy);

        Assert.Equal(0, x);
        Assert.Equal(0, y);
    }

    [Fact]
    public void EveryStepOfTheCircleActuallyMovesTheCursor()
    {
        Assert.NotEmpty(CircularNudge.Path);

        // Windows does not count a zero-pixel move as input, so such a step would achieve nothing.
        Assert.All(CircularNudge.Path, step => Assert.True(step.Dx != 0 || step.Dy != 0));
    }

    [Fact]
    public void NothingHappensWhileTheFeatureIsOff()
    {
        var (service, mouse, _, _, _) = Create(enabled: false, idle: TimeSpan.FromHours(1));

        RunTicks(service, 200);

        Assert.Empty(mouse.Movements);
        Assert.Equal(0, service.NudgeCount);
    }

    [Fact]
    public void AnIdleMachineIsNudgedOnce()
    {
        var (service, mouse, _, _, _) = Create(enabled: true, idle: TimeSpan.FromSeconds(90));

        RunTicks(service, CircularNudge.Path.Count);

        Assert.Equal(1, service.NudgeCount);
        Assert.Equal(CircularNudge.Path.Count, mouse.Movements.Count);
        Assert.False(service.IsNudging);
    }

    [Fact]
    public void AMachineTheUserIsStillUsingIsLeftAlone()
    {
        var (service, mouse, _, _, _) = Create(enabled: true, idle: TimeSpan.FromSeconds(5));

        RunTicks(service, 500);

        Assert.Empty(mouse.Movements);
        Assert.Equal(0, service.NudgeCount);
    }

    [Fact]
    public void TheCursorEndsWhereItStartedAfterANudge()
    {
        var (service, mouse, _, _, _) = Create(enabled: true, idle: TimeSpan.FromSeconds(90));

        RunTicks(service, CircularNudge.Path.Count);

        Assert.Equal(0, mouse.Movements.Sum(move => move.Dx));
        Assert.Equal(0, mouse.Movements.Sum(move => move.Dy));
    }

    [Fact]
    public void TheIntervalIsRespectedRatherThanNudgingEveryTick()
    {
        var (service, _, activity, clock, _) = Create(enabled: true, idle: TimeSpan.FromSeconds(90));

        RunTicks(service, CircularNudge.Path.Count);
        Assert.Equal(1, service.NudgeCount);

        // Windows still reports the machine as idle, which is what would cause a runaway.
        activity.IdleTime = TimeSpan.FromSeconds(90);
        clock.Advance(TimeSpan.FromSeconds(30));
        RunTicks(service, 500);

        Assert.Equal(1, service.NudgeCount);

        clock.Advance(TimeSpan.FromSeconds(31));
        RunTicks(service, CircularNudge.Path.Count);

        Assert.Equal(2, service.NudgeCount);
    }

    [Fact]
    public void AShorterIntervalNudgesSooner()
    {
        var (service, _, activity, clock, _) = Create(enabled: true, idle: TimeSpan.FromSeconds(20), interval: 15);

        RunTicks(service, CircularNudge.Path.Count);
        Assert.Equal(1, service.NudgeCount);

        activity.IdleTime = TimeSpan.FromSeconds(20);
        clock.Advance(TimeSpan.FromSeconds(16));
        RunTicks(service, CircularNudge.Path.Count);

        Assert.Equal(2, service.NudgeCount);
    }

    [Fact]
    public void AGameOwningTheControllerIsNeverNudged()
    {
        var (service, mouse, _, _, _) = Create(enabled: true, idle: TimeSpan.FromSeconds(90));

        RunTicks(service, 500, allowed: false);

        Assert.Empty(mouse.Movements);
    }

    [Fact]
    public void ACircleAlreadyUnderWayFinishesEvenIfAGameTakesFocus()
    {
        var (service, mouse, _, _, _) = Create(enabled: true, idle: TimeSpan.FromSeconds(90));

        service.Update(allowed: true);
        Assert.True(service.IsNudging);

        RunTicks(service, CircularNudge.Path.Count, allowed: false);

        // Abandoning it half way would strand the cursor away from where the user left it.
        Assert.Equal(0, mouse.Movements.Sum(move => move.Dx));
        Assert.Equal(0, mouse.Movements.Sum(move => move.Dy));
        Assert.False(service.IsNudging);
    }

    [Fact]
    public void TurningTheFeatureOffMidCircleStopsImmediately()
    {
        var (service, mouse, _, _, settings) = Create(enabled: true, idle: TimeSpan.FromSeconds(90));

        service.Update(allowed: true);
        Assert.True(service.IsNudging);

        settings.KeepScreenOnEnabled = false;
        service.Update(allowed: true);

        Assert.False(service.IsNudging);
        Assert.Single(mouse.Movements);
    }

    private static void RunTicks(KeepAwakeService service, int ticks, bool allowed = true)
    {
        for (var tick = 0; tick < ticks; tick++)
        {
            service.Update(allowed);
        }
    }

    private static (KeepAwakeService Service, RecordingMouse Mouse, FakeActivity Activity, FakeClock Clock, AppSettings Settings) Create(
        bool enabled,
        TimeSpan idle,
        int interval = 60)
    {
        var settings = AppSettings.CreateDefault();
        settings.KeepScreenOnEnabled = enabled;
        settings.KeepScreenOnIntervalSeconds = interval;

        var mouse = new RecordingMouse();
        var activity = new FakeActivity { IdleTime = idle };
        var clock = new FakeClock();

        return (new KeepAwakeService(settings, mouse, activity, () => clock.Now), mouse, activity, clock, settings);
    }

    private sealed class FakeActivity : IUserActivitySource
    {
        public TimeSpan IdleTime { get; set; }
    }

    private sealed class FakeClock
    {
        public DateTime Now { get; private set; } = Start;

        public void Advance(TimeSpan amount) => Now += amount;
    }
}
