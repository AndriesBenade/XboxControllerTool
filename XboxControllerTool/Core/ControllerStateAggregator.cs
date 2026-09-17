namespace XboxControllerTool.Core;

public static class ControllerStateAggregator
{
    public static ControllerState Combine(IReadOnlyList<ControllerState> accepted)
    {
        if (accepted.Count == 0)
        {
            return ControllerState.Disconnected(-1);
        }

        if (accepted.Count == 1)
        {
            return accepted[0];
        }

        var buttons = GamepadButton.None;
        var dominant = accepted[0];
        var dominantActivity = Activity(dominant);

        foreach (var state in accepted)
        {
            buttons |= state.Buttons;
            var activity = Activity(state);
            if (activity > dominantActivity)
            {
                dominant = state;
                dominantActivity = activity;
            }
        }

        return dominant with { Buttons = buttons };
    }

    private static int Activity(ControllerState state) =>
        Math.Abs((int)state.LeftThumbX) + Math.Abs((int)state.LeftThumbY) +
        Math.Abs((int)state.RightThumbX) + Math.Abs((int)state.RightThumbY) +
        state.LeftTrigger + state.RightTrigger;
}
