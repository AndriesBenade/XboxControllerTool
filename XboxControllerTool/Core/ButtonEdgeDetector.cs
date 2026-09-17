namespace XboxControllerTool.Core;

public readonly record struct ButtonTransitions(GamepadButton Pressed, GamepadButton Released)
{
    public bool WasPressed(GamepadButton button) => (Pressed & button) == button;
    public bool WasReleased(GamepadButton button) => (Released & button) == button;
}

public static class ButtonEdgeDetector
{
    public static ButtonTransitions Detect(GamepadButton previous, GamepadButton current)
    {
        var pressed = current & ~previous;
        var released = previous & ~current;
        return new ButtonTransitions(pressed, released);
    }
}
