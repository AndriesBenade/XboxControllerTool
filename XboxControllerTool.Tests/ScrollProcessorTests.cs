using XboxControllerTool.Processing;

namespace XboxControllerTool.Tests;

public class ScrollProcessorTests
{
    [Fact]
    public void ComputeWheelDelta_WithinDeadZone_ProducesNoScroll()
    {
        var processor = new ScrollProcessor();

        var delta = processor.ComputeWheelDelta(2000, deadZone: 0.2, sensitivity: 9, speedMultiplier: 1.0);

        Assert.Equal(0, delta);
    }

    [Fact]
    public void ComputeWheelDelta_FullDeflection_ProducesOutputOnTheFirstTick()
    {
        var processor = new ScrollProcessor();

        var firstTickDelta = processor.ComputeWheelDelta(32767, deadZone: 0.15, sensitivity: 9, speedMultiplier: 1.0);

        Assert.NotEqual(0, firstTickDelta);
    }

    [Fact]
    public void ComputeWheelDelta_LowSpeedMultiplier_ProducesSmoothSubNotchIncrements()
    {
        var processor = new ScrollProcessor();

        var delta = processor.ComputeWheelDelta(32767, deadZone: 0.15, sensitivity: 9, speedMultiplier: 0.15);

        Assert.NotEqual(0, delta);
        Assert.True(Math.Abs(delta) < 120, "Slow scrolling should move in small smooth increments, not full notch jumps.");
    }

    [Fact]
    public void ComputeWheelDelta_LowSpeedMultiplier_AccumulatesLessThanNormalMultiplier()
    {
        var normalProcessor = new ScrollProcessor();
        var slowProcessor = new ScrollProcessor();

        var normalTotal = AccumulateOverTicks(normalProcessor, speedMultiplier: 1.0);
        var slowTotal = AccumulateOverTicks(slowProcessor, speedMultiplier: 0.15);

        Assert.True(slowTotal < normalTotal);
    }

    [Fact]
    public void ComputeWheelDelta_HigherSpeedMultiplier_AccumulatesMoreThanNormalMultiplier()
    {
        var normalProcessor = new ScrollProcessor();
        var fasterProcessor = new ScrollProcessor();

        var normalTotal = AccumulateOverTicks(normalProcessor, speedMultiplier: 1.0);
        var fasterTotal = AccumulateOverTicks(fasterProcessor, speedMultiplier: 2.2);

        Assert.True(fasterTotal > normalTotal);
    }

    [Fact]
    public void ComputeWheelDelta_NegativeStick_ProducesNegativeWheelDelta()
    {
        var processor = new ScrollProcessor();

        var total = 0;
        for (var i = 0; i < 200 && total == 0; i++)
        {
            total += processor.ComputeWheelDelta(-32767, deadZone: 0.15, sensitivity: 9, speedMultiplier: 1.0);
        }

        Assert.True(total < 0);
    }

    private static int AccumulateOverTicks(ScrollProcessor processor, double speedMultiplier, int ticks = 50)
    {
        var total = 0;
        for (var i = 0; i < ticks; i++)
        {
            total += processor.ComputeWheelDelta(32767, deadZone: 0.15, sensitivity: 9, speedMultiplier);
        }

        return total;
    }
}
