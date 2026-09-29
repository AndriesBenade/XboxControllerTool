using XboxControllerTool.Configuration;
using XboxControllerTool.Simulation;
using XboxControllerTool.Windows;

namespace XboxControllerTool.Application;

/// <summary>
/// The small circle the cursor is walked around to register as activity.
/// <para>
/// The steps are stored as relative moves that sum to zero, so the cursor finishes exactly where it
/// started however many times this runs. A single jump and back would be cheaper but reads as a
/// glitch; a slow circle looks like a hand resting on a mouse.
/// </para>
/// </summary>
public static class CircularNudge
{
    private const int Radius = 3;
    private const int Steps = 16;

    public static IReadOnlyList<(int Dx, int Dy)> Path { get; } = Build();

    private static (int Dx, int Dy)[] Build()
    {
        var points = new (int X, int Y)[Steps];

        for (var step = 0; step < Steps; step++)
        {
            var angle = 2 * Math.PI * step / Steps;
            points[step] = ((int)Math.Round(Radius * Math.Cos(angle)), (int)Math.Round(Radius * Math.Sin(angle)));
        }

        var deltas = new List<(int Dx, int Dy)>(Steps);

        for (var step = 0; step < Steps; step++)
        {
            var from = points[step];
            var to = points[(step + 1) % Steps];
            var delta = (Dx: to.X - from.X, Dy: to.Y - from.Y);

            // A zero move is not input as far as Windows is concerned, so it would not count.
            if (delta is not (0, 0))
            {
                deltas.Add(delta);
            }
        }

        return [.. deltas];
    }
}

/// <summary>
/// Keeps the machine awake by behaving like someone who is still there.
/// <para>
/// Once the interval passes with no input at all, the cursor is walked around a small circle. That
/// counts as real user activity to Windows, so it holds off the screen blanking, sleep, and any
/// idle or away status other software works out from the same clock. If the user did anything in
/// that window, nothing happens.
/// </para>
/// </summary>
public sealed class KeepAwakeService
{
    private const int NotNudging = -1;

    private readonly AppSettings _settings;
    private readonly IMouseInput _mouse;
    private readonly IUserActivitySource _activity;
    private readonly Func<DateTime> _clock;

    private int _step = NotNudging;
    private DateTime _lastNudgeUtc = DateTime.MinValue;

    public KeepAwakeService(
        AppSettings settings,
        IMouseInput mouse,
        IUserActivitySource activity,
        Func<DateTime>? clock = null)
    {
        _settings = settings;
        _mouse = mouse;
        _activity = activity;
        _clock = clock ?? (() => DateTime.UtcNow);
    }

    public bool IsNudging => _step != NotNudging;

    /// <summary>How many times the cursor has been nudged since the app started.</summary>
    public int NudgeCount { get; private set; }

    /// <summary>
    /// Advances by one tick. <paramref name="allowed"/> is false while a game owns the controller,
    /// because moving the mouse into a game would swing the camera; a circle already under way still
    /// finishes, so the cursor is never abandoned away from where it started.
    /// </summary>
    public void Update(bool allowed)
    {
        if (!_settings.KeepScreenOnEnabled)
        {
            _step = NotNudging;
            return;
        }

        if (IsNudging)
        {
            MoveOneStep();
            return;
        }

        if (!allowed)
        {
            return;
        }

        var interval = TimeSpan.FromSeconds(_settings.KeepScreenOnIntervalSeconds);
        var now = _clock();

        // The second guard matters if a nudge somehow fails to register as input: without it the
        // cursor would be walked in circles on every single tick.
        if (_activity.IdleTime < interval || now - _lastNudgeUtc < interval)
        {
            return;
        }

        _lastNudgeUtc = now;
        NudgeCount++;
        _step = 0;
        MoveOneStep();
    }

    /// <summary>
    /// One step per tick rather than the whole circle at once: the input loop runs every few
    /// milliseconds and must not be blocked, and spreading it out is what makes the motion look
    /// like a movement instead of a jump.
    /// </summary>
    private void MoveOneStep()
    {
        var (dx, dy) = CircularNudge.Path[_step];
        _mouse.MoveRelative(dx, dy);

        _step++;

        if (_step >= CircularNudge.Path.Count)
        {
            _step = NotNudging;
        }
    }
}
