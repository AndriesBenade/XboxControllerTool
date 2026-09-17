using System.Numerics;
using XboxControllerTool.Processing;

namespace XboxControllerTool.Tests;

public class MouseMovementProcessorTests
{
    [Fact]
    public void ComputeMovement_ZeroInput_ProducesNoMovement()
    {
        var processor = new MouseMovementProcessor();

        var (dx, dy) = processor.ComputeMovement(Vector2.Zero, sensitivity: 14, speedMultiplier: 1.0);

        Assert.Equal(0, dx);
        Assert.Equal(0, dy);
    }

    [Fact]
    public void ComputeMovement_LowSpeedMultiplier_MovesSlowerThanNormalMultiplier()
    {
        var processor = new MouseMovementProcessor();
        var stick = new Vector2(1f, 0f);

        var normalTotal = 0;
        var slowTotal = 0;

        for (var i = 0; i < 50; i++)
        {
            var (dx, _) = processor.ComputeMovement(stick, sensitivity: 14, speedMultiplier: 1.0);
            normalTotal += dx;
        }

        processor.Reset();

        for (var i = 0; i < 50; i++)
        {
            var (dx, _) = processor.ComputeMovement(stick, sensitivity: 14, speedMultiplier: 0.25);
            slowTotal += dx;
        }

        Assert.True(slowTotal < normalTotal);
    }

    [Fact]
    public void ComputeMovement_HigherSpeedMultiplier_MovesFurtherThanNormalMultiplier()
    {
        var processor = new MouseMovementProcessor();
        var stick = new Vector2(0.3f, 0f);

        var normalTotal = 0;
        var fasterTotal = 0;

        for (var i = 0; i < 50; i++)
        {
            var (dx, _) = processor.ComputeMovement(stick, sensitivity: 14, speedMultiplier: 1.0);
            normalTotal += dx;
        }

        processor.Reset();

        for (var i = 0; i < 50; i++)
        {
            var (dx, _) = processor.ComputeMovement(stick, sensitivity: 14, speedMultiplier: 2.2);
            fasterTotal += dx;
        }

        Assert.True(fasterTotal > normalTotal);
    }

    [Fact]
    public void ComputeMovement_SmallSustainedInput_AccumulatesWithoutLosingFractionalMovement()
    {
        var processor = new MouseMovementProcessor();
        var stick = new Vector2(0.05f, 0f);

        var total = 0;
        for (var i = 0; i < 200; i++)
        {
            var (dx, _) = processor.ComputeMovement(stick, sensitivity: 14, speedMultiplier: 1.0);
            total += dx;
        }

        Assert.True(total > 0);
    }

    [Fact]
    public void ComputeMovement_StickUp_MovesCursorNegativeY()
    {
        var processor = new MouseMovementProcessor();
        var stick = new Vector2(0f, 1f);

        var (_, dy) = processor.ComputeMovement(stick, sensitivity: 14, speedMultiplier: 1.0);

        Assert.True(dy <= 0);
    }
}
