using XboxControllerTool.Application;
using XboxControllerTool.Core;
using XboxControllerTool.Windows.RawInput;

namespace XboxControllerTool.Tests;

public class RawButtonCorrelatorTests
{
    private static readonly DateTime Start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void APressIsHeldBackUntilTheCorrelationWindowHasElapsed()
    {
        var correlator = new RawButtonCorrelator();
        correlator.NoteRawPress(Raw(Start, 15));

        Assert.Empty(correlator.Release(Start));
        Assert.Empty(correlator.Release(Start + RawButtonCorrelator.CorrelationWindow - TimeSpan.FromMilliseconds(1)));

        var released = correlator.Release(Start + RawButtonCorrelator.CorrelationWindow);

        Assert.Equal((ushort)15, Assert.Single(released).Usage);
    }

    [Fact]
    public void APressReleasedOnceIsNotReleasedAgain()
    {
        var correlator = new RawButtonCorrelator();
        correlator.NoteRawPress(Raw(Start, 15));

        Assert.Single(correlator.Release(Start + TimeSpan.FromMilliseconds(100)));
        Assert.Empty(correlator.Release(Start + TimeSpan.FromMilliseconds(200)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(-50)]
    public void APressMatchedByAnXInputPressIsDiscardedAsADuplicate(int controllerOffsetMs)
    {
        var correlator = new RawButtonCorrelator();
        correlator.NoteControllerPress(Start + TimeSpan.FromMilliseconds(controllerOffsetMs));
        correlator.NoteRawPress(Raw(Start, 1));

        Assert.Empty(correlator.Release(Start + TimeSpan.FromMilliseconds(200)));
    }

    [Fact]
    public void AnXInputPressWellOutsideTheWindowDoesNotSuppressAnExtraButton()
    {
        var correlator = new RawButtonCorrelator();
        correlator.NoteControllerPress(Start);
        correlator.NoteRawPress(Raw(Start + TimeSpan.FromMilliseconds(500), 15));

        var released = correlator.Release(Start + TimeSpan.FromMilliseconds(600));

        Assert.Single(released);
    }

    [Fact]
    public void ReleasedPressesKeepTheOrderTheyWereObservedIn()
    {
        var correlator = new RawButtonCorrelator();
        correlator.NoteRawPress(Raw(Start, 15));
        correlator.NoteRawPress(Raw(Start + TimeSpan.FromMilliseconds(5), 16));
        correlator.NoteRawPress(Raw(Start + TimeSpan.FromMilliseconds(10), 17));

        var released = correlator.Release(Start + TimeSpan.FromMilliseconds(200));

        Assert.Equal<ushort[]>([15, 16, 17], [.. released.Select(press => press.Usage)]);
    }

    [Fact]
    public void OneXInputPressOnlySuppressesTheRawPressItLinesUpWith()
    {
        var correlator = new RawButtonCorrelator();
        correlator.NoteControllerPress(Start);
        correlator.NoteRawPress(Raw(Start, 1));
        correlator.NoteRawPress(Raw(Start + TimeSpan.FromMilliseconds(400), 15));

        var released = correlator.Release(Start + TimeSpan.FromMilliseconds(500));

        Assert.Equal((ushort)15, Assert.Single(released).Usage);
    }

    private static RawGamepadButtonPress Raw(DateTime observedUtc, ushort usage) =>
        new("045E-0B12", usage, observedUtc);
}
