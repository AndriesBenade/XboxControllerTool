using XboxControllerTool.Application;
using XboxControllerTool.ConsoleUi;
using XboxControllerTool.Core;

namespace XboxControllerTool.Tests;

public class MenuInputTranslatorTests
{
    [Fact]
    public void ComputeDigitalDirections_StickBelowThreshold_ProducesNoDirection()
    {
        var state = new ControllerState(0, true, 1, GamepadButton.None, 10000, 0, 0, 0, 0, 0);

        var directions = MenuInputTranslator.ComputeDigitalDirections(state);

        Assert.Equal(GamepadButton.None, directions);
    }

    [Fact]
    public void ComputeDigitalDirections_StickPastThreshold_MapsToDPadDirection()
    {
        var state = new ControllerState(0, true, 1, GamepadButton.None, 0, 30000, 0, 0, 0, 0);

        var directions = MenuInputTranslator.ComputeDigitalDirections(state);

        Assert.Equal(GamepadButton.DPadUp, directions);
    }

    [Fact]
    public void ComputeDigitalDirections_RealDPadInput_IsPreserved()
    {
        var state = new ControllerState(0, true, 1, GamepadButton.DPadRight, 0, 0, 0, 0, 0, 0);

        var directions = MenuInputTranslator.ComputeDigitalDirections(state);

        Assert.Equal(GamepadButton.DPadRight, directions);
    }

    [Fact]
    public void ToPrimaryAction_APress_ReturnsConfirm()
    {
        var transitions = ButtonEdgeDetector.Detect(GamepadButton.None, GamepadButton.A);

        var action = MenuInputTranslator.ToPrimaryAction(transitions, GamepadButton.None);

        Assert.Equal(MenuAction.Confirm, action);
    }

    [Fact]
    public void ToPrimaryAction_BPress_ReturnsCancel()
    {
        var transitions = ButtonEdgeDetector.Detect(GamepadButton.None, GamepadButton.B);

        var action = MenuInputTranslator.ToPrimaryAction(transitions, GamepadButton.None);

        Assert.Equal(MenuAction.Cancel, action);
    }

    [Fact]
    public void ToPrimaryAction_NoInput_ReturnsNone()
    {
        var transitions = ButtonEdgeDetector.Detect(GamepadButton.None, GamepadButton.None);

        var action = MenuInputTranslator.ToPrimaryAction(transitions, GamepadButton.None);

        Assert.Equal(MenuAction.None, action);
    }
}
