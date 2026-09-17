namespace XboxControllerTool.Processing;

public sealed class ScrollProcessor
{
    private const int WheelDeltaUnit = 120;
    private const double ResponseGain = 0.07;

    private double _accumulatedUnits;

    public int ComputeWheelDelta(short rawY, double deadZone, double sensitivity, double speedMultiplier)
    {
        var normalized = rawY / 32767.0;
        var magnitude = Math.Abs(normalized);

        if (magnitude < deadZone)
        {
            return 0;
        }

        var clippedMagnitude = Math.Min(magnitude, 1.0);
        var normalizedMagnitude = (clippedMagnitude - deadZone) / (1.0 - deadZone);
        var direction = Math.Sign(normalized);

        var notchesPerTick = direction * normalizedMagnitude * sensitivity * speedMultiplier * ResponseGain;
        _accumulatedUnits += notchesPerTick * WheelDeltaUnit;

        var wholeUnits = Math.Truncate(_accumulatedUnits);
        _accumulatedUnits -= wholeUnits;

        return (int)wholeUnits;
    }

    public void Reset()
    {
        _accumulatedUnits = 0;
    }
}
