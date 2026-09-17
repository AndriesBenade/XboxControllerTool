using XboxControllerTool.Core;

namespace XboxControllerTool.Tests;

public class ControllerStateAggregatorTests
{
    [Fact]
    public void Combine_NoControllers_ReturnsDisconnectedState()
    {
        var result = ControllerStateAggregator.Combine([]);

        Assert.False(result.IsConnected);
    }

    [Fact]
    public void Combine_SingleController_ReturnsItUnchanged()
    {
        var state = new ControllerState(0, true, 1, GamepadButton.A, 0, 0, 0, 0, 0, 0);

        var result = ControllerStateAggregator.Combine([state]);

        Assert.Equal(state, result);
    }

    [Fact]
    public void Combine_MultipleControllers_MergesButtonsAcrossAll()
    {
        var first = new ControllerState(0, true, 1, GamepadButton.A, 0, 0, 0, 0, 0, 0);
        var second = new ControllerState(1, true, 1, GamepadButton.B, 0, 0, 0, 0, 0, 0);

        var result = ControllerStateAggregator.Combine([first, second]);

        Assert.True(result.IsPressed(GamepadButton.A));
        Assert.True(result.IsPressed(GamepadButton.B));
    }

    [Fact]
    public void Combine_MultipleControllers_UsesStickInputFromMostActiveController()
    {
        var idle = new ControllerState(0, true, 1, GamepadButton.None, 0, 0, 0, 0, 0, 0);
        var active = new ControllerState(1, true, 1, GamepadButton.None, 20000, -5000, 0, 0, 0, 0);

        var result = ControllerStateAggregator.Combine([idle, active]);

        Assert.Equal(20000, result.LeftThumbX);
        Assert.Equal(-5000, result.LeftThumbY);
    }
}
