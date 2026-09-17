using System.Numerics;

namespace XboxControllerTool.Processing;

public sealed class MouseMovementProcessor
{
    private const double MaxPixelsPerTick = 55.0;

    private double _accumulatedX;
    private double _accumulatedY;

    public (int Dx, int Dy) ComputeMovement(Vector2 processedStick, double sensitivity, double speedMultiplier)
    {
        var pixelsX = Math.Clamp(processedStick.X * sensitivity * speedMultiplier, -MaxPixelsPerTick, MaxPixelsPerTick);
        var pixelsY = Math.Clamp(-processedStick.Y * sensitivity * speedMultiplier, -MaxPixelsPerTick, MaxPixelsPerTick);

        _accumulatedX += pixelsX;
        _accumulatedY += pixelsY;

        var dx = (int)_accumulatedX;
        var dy = (int)_accumulatedY;

        _accumulatedX -= dx;
        _accumulatedY -= dy;

        return (dx, dy);
    }

    public void Reset()
    {
        _accumulatedX = 0;
        _accumulatedY = 0;
    }
}
