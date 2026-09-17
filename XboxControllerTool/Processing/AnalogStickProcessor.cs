using System.Numerics;

namespace XboxControllerTool.Processing;

public static class AnalogStickProcessor
{
    public static Vector2 Process(short rawX, short rawY, double deadZone, bool accelerationEnabled)
    {
        var x = rawX / 32767.0;
        var y = rawY / 32767.0;
        var magnitude = Math.Sqrt(x * x + y * y);

        if (magnitude < deadZone)
        {
            return Vector2.Zero;
        }

        var clippedMagnitude = Math.Min(magnitude, 1.0);
        var normalizedMagnitude = (clippedMagnitude - deadZone) / (1.0 - deadZone);
        var curvedMagnitude = accelerationEnabled ? normalizedMagnitude * normalizedMagnitude : normalizedMagnitude;
        var scale = curvedMagnitude / magnitude;

        return new Vector2((float)(x * scale), (float)(y * scale));
    }
}
