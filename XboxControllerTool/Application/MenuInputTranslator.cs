using XboxControllerTool.ConsoleUi;
using XboxControllerTool.Core;

namespace XboxControllerTool.Application;

public static class MenuInputTranslator
{
    private const double StickThreshold = 0.5;

    public static GamepadButton ComputeDigitalDirections(ControllerState state)
    {
        var directions = state.Buttons & (GamepadButton.DPadUp | GamepadButton.DPadDown | GamepadButton.DPadLeft | GamepadButton.DPadRight);

        var x = state.LeftThumbX / 32767.0;
        var y = state.LeftThumbY / 32767.0;

        if (y > StickThreshold)
        {
            directions |= GamepadButton.DPadUp;
        }

        if (y < -StickThreshold)
        {
            directions |= GamepadButton.DPadDown;
        }

        if (x < -StickThreshold)
        {
            directions |= GamepadButton.DPadLeft;
        }

        if (x > StickThreshold)
        {
            directions |= GamepadButton.DPadRight;
        }

        return directions;
    }

    public static MenuAction ToPrimaryAction(ButtonTransitions faceButtonTransitions, GamepadButton pressedDirections)
    {
        if (faceButtonTransitions.WasPressed(GamepadButton.A))
        {
            return MenuAction.Confirm;
        }

        if (faceButtonTransitions.WasPressed(GamepadButton.B))
        {
            return MenuAction.Cancel;
        }

        if (faceButtonTransitions.WasPressed(GamepadButton.X))
        {
            return MenuAction.Clear;
        }

        if ((pressedDirections & GamepadButton.DPadUp) != 0)
        {
            return MenuAction.Up;
        }

        if ((pressedDirections & GamepadButton.DPadDown) != 0)
        {
            return MenuAction.Down;
        }

        if ((pressedDirections & GamepadButton.DPadLeft) != 0)
        {
            return MenuAction.Left;
        }

        if ((pressedDirections & GamepadButton.DPadRight) != 0)
        {
            return MenuAction.Right;
        }

        return MenuAction.None;
    }
}
