using XboxControllerTool.Processing;

namespace XboxControllerTool.Tests;

public class AnalogStickProcessorTests
{
    [Fact]
    public void Process_ValueWithinDeadZone_ProducesZeroVector()
    {
        var result = AnalogStickProcessor.Process(1000, 1000, deadZone: 0.2, accelerationEnabled: false);

        Assert.Equal(0f, result.X);
        Assert.Equal(0f, result.Y);
    }

    [Fact]
    public void Process_FullDeflection_ProducesUnitMagnitude()
    {
        var result = AnalogStickProcessor.Process(32767, 0, deadZone: 0.2, accelerationEnabled: false);

        Assert.InRange(result.Length(), 0.99f, 1.01f);
    }

    [Fact]
    public void Process_JustPastDeadZone_ProducesSmallNonZeroOutput()
    {
        short raw = (short)(32767 * 0.25);

        var result = AnalogStickProcessor.Process(raw, 0, deadZone: 0.2, accelerationEnabled: false);

        Assert.True(result.X > 0f);
        Assert.True(result.X < 0.2f);
    }

    [Fact]
    public void Process_AccelerationCurve_ReducesMidRangeOutputComparedToLinear()
    {
        short raw = (short)(32767 * 0.6);

        var linear = AnalogStickProcessor.Process(raw, 0, deadZone: 0.2, accelerationEnabled: false);
        var accelerated = AnalogStickProcessor.Process(raw, 0, deadZone: 0.2, accelerationEnabled: true);

        Assert.True(accelerated.X < linear.X);
    }

    [Fact]
    public void Process_PreservesDirection()
    {
        var result = AnalogStickProcessor.Process(-32767, -32767, deadZone: 0.2, accelerationEnabled: false);

        Assert.True(result.X < 0);
        Assert.True(result.Y < 0);
    }
}
