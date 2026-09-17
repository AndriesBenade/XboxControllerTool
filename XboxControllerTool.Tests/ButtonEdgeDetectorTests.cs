using XboxControllerTool.Core;

namespace XboxControllerTool.Tests;

public class ButtonEdgeDetectorTests
{
    [Fact]
    public void Detect_ReportsNewlyPressedButtons()
    {
        var transitions = ButtonEdgeDetector.Detect(GamepadButton.None, GamepadButton.A);

        Assert.True(transitions.WasPressed(GamepadButton.A));
        Assert.False(transitions.WasReleased(GamepadButton.A));
    }

    [Fact]
    public void Detect_ReportsNewlyReleasedButtons()
    {
        var transitions = ButtonEdgeDetector.Detect(GamepadButton.A, GamepadButton.None);

        Assert.True(transitions.WasReleased(GamepadButton.A));
        Assert.False(transitions.WasPressed(GamepadButton.A));
    }

    [Fact]
    public void Detect_HeldButtonProducesNoTransition()
    {
        var transitions = ButtonEdgeDetector.Detect(GamepadButton.A, GamepadButton.A);

        Assert.False(transitions.WasPressed(GamepadButton.A));
        Assert.False(transitions.WasReleased(GamepadButton.A));
    }

    [Fact]
    public void Detect_HoldingButtonAcrossManyTicksNeverRepeatsPress()
    {
        var previous = GamepadButton.A;

        for (var i = 0; i < 500; i++)
        {
            var transitions = ButtonEdgeDetector.Detect(previous, GamepadButton.A);
            Assert.False(transitions.WasPressed(GamepadButton.A));
            previous = GamepadButton.A;
        }
    }

    [Fact]
    public void Detect_TracksMultipleButtonsIndependently()
    {
        var transitions = ButtonEdgeDetector.Detect(GamepadButton.A, GamepadButton.A | GamepadButton.B);

        Assert.True(transitions.WasPressed(GamepadButton.B));
        Assert.False(transitions.WasPressed(GamepadButton.A));
    }
}
