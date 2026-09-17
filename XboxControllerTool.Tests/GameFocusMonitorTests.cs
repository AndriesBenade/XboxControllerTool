using XboxControllerTool.Application;
using XboxControllerTool.Windows;

namespace XboxControllerTool.Tests;

public class GameFocusMonitorTests
{
    private const int GamePid = 1000;
    private const int BrowserPid = 2000;
    private const int OwnPid = 9000;

    private sealed class FakeForeground : IForegroundWindowSource
    {
        public int ProcessId { get; set; }

        public ForegroundWindowInfo GetForeground() => new(new nint(ProcessId), ProcessId);
    }

    private sealed class FakeClassifier : IGameClassifier
    {
        public int CallCount { get; private set; }

        public GameClassification Classify(int processId)
        {
            CallCount++;

            return processId == GamePid
                ? new GameClassification(true, "minecraft", "uses controller runtime (xinput1_4.dll)")
                : new GameClassification(false, "chrome", "no game signal");
        }
    }

    [Fact]
    public void FocusingAGame_PausesDesktopInput()
    {
        var (monitor, foreground, _) = Create();
        foreground.ProcessId = GamePid;

        var transition = monitor.Update(detectionEnabled: true);

        Assert.Equal(GameFocusTransition.GameFocused, transition);
        Assert.True(monitor.IsGameFocused);
        Assert.Equal("minecraft", monitor.FocusedProcessName);
    }

    [Fact]
    public void AltTabbingOutOfAGame_ResumesDesktopInput()
    {
        var (monitor, foreground, _) = Create();

        foreground.ProcessId = GamePid;
        monitor.Update(detectionEnabled: true);

        foreground.ProcessId = BrowserPid;
        var transition = monitor.Update(detectionEnabled: true);

        Assert.Equal(GameFocusTransition.GameUnfocused, transition);
        Assert.False(monitor.IsGameFocused);
    }

    [Fact]
    public void AltTabbingBackIntoAGame_PausesDesktopInputAgain()
    {
        var (monitor, foreground, _) = Create();

        foreground.ProcessId = GamePid;
        monitor.Update(detectionEnabled: true);
        foreground.ProcessId = BrowserPid;
        monitor.Update(detectionEnabled: true);

        foreground.ProcessId = GamePid;
        var transition = monitor.Update(detectionEnabled: true);

        Assert.Equal(GameFocusTransition.GameFocused, transition);
        Assert.True(monitor.IsGameFocused);
    }

    [Fact]
    public void GameRunningInBackground_LeavesDesktopInputActive()
    {
        var (monitor, foreground, _) = Create();
        foreground.ProcessId = BrowserPid;

        var transition = monitor.Update(detectionEnabled: true);

        Assert.Equal(GameFocusTransition.None, transition);
        Assert.False(monitor.IsGameFocused);
    }

    [Fact]
    public void StayingInsideAGame_DoesNotRepeatTheTransition()
    {
        var (monitor, foreground, _) = Create();
        foreground.ProcessId = GamePid;

        var first = monitor.Update(detectionEnabled: true);
        var repeats = Enumerable.Range(0, 50).Select(_ => monitor.Update(detectionEnabled: true)).ToList();

        Assert.Equal(GameFocusTransition.GameFocused, first);
        Assert.All(repeats, transition => Assert.Equal(GameFocusTransition.None, transition));
        Assert.True(monitor.IsGameFocused);
    }

    [Fact]
    public void PositiveClassificationIsCached_SoDetectionDoesNotRescanEveryPoll()
    {
        var (monitor, foreground, classifier) = Create();
        foreground.ProcessId = GamePid;

        for (var i = 0; i < 25; i++)
        {
            monitor.Update(detectionEnabled: true);
        }

        Assert.Equal(1, classifier.CallCount);
    }

    [Fact]
    public void NegativeClassificationIsRetried_SoAGameStillStartingIsNotMissedForever()
    {
        var now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var foreground = new FakeForeground { ProcessId = BrowserPid };
        var classifier = new FakeClassifier();
        var monitor = new GameFocusMonitor(foreground, classifier, () => now, OwnPid);

        monitor.Update(detectionEnabled: true);
        now = now.AddSeconds(30);
        monitor.Update(detectionEnabled: true);

        Assert.Equal(2, classifier.CallCount);
    }

    [Fact]
    public void OwnApplicationInForeground_IsNeverTreatedAsAGame()
    {
        var (monitor, foreground, classifier) = Create();
        foreground.ProcessId = OwnPid;

        var transition = monitor.Update(detectionEnabled: true);

        Assert.Equal(GameFocusTransition.None, transition);
        Assert.False(monitor.IsGameFocused);
        Assert.Equal(0, classifier.CallCount);
    }

    [Fact]
    public void DetectionDisabled_ReleasesAnExistingPauseAndStopsClassifying()
    {
        var (monitor, foreground, classifier) = Create();
        foreground.ProcessId = GamePid;
        monitor.Update(detectionEnabled: true);

        var callsBefore = classifier.CallCount;
        var transition = monitor.Update(detectionEnabled: false);

        Assert.Equal(GameFocusTransition.GameUnfocused, transition);
        Assert.False(monitor.IsGameFocused);
        Assert.Equal(callsBefore, classifier.CallCount);
    }

    private static (GameFocusMonitor Monitor, FakeForeground Foreground, FakeClassifier Classifier) Create()
    {
        var foreground = new FakeForeground();
        var classifier = new FakeClassifier();
        return (new GameFocusMonitor(foreground, classifier, () => DateTime.UtcNow, OwnPid), foreground, classifier);
    }
}
