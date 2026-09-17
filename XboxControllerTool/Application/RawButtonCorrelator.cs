using XboxControllerTool.Windows.RawInput;

namespace XboxControllerTool.Application;

/// <summary>
/// Decides which raw HID button presses represent buttons XInput cannot see.
/// <para>
/// Windows delivers HID reports for XInput pads as well, so every ordinary face button produces
/// both an XInput edge and a HID edge. A HID press is therefore held back briefly and discarded if
/// an XInput press arrived at roughly the same moment, on the assumption that both describe the
/// same physical button. What survives the wait is a button XInput never reported at all.
/// </para>
/// </summary>
public sealed class RawButtonCorrelator
{
    public static readonly TimeSpan CorrelationWindow = TimeSpan.FromMilliseconds(60);

    private static readonly TimeSpan PendingLifetime = TimeSpan.FromSeconds(2);

    private readonly List<DateTime> _controllerPresses = [];
    private readonly List<RawGamepadButtonPress> _pending = [];

    public void NoteControllerPress(DateTime observedUtc) => _controllerPresses.Add(observedUtc);

    public void NoteRawPress(RawGamepadButtonPress press) => _pending.Add(press);

    /// <summary>
    /// Returns the raw presses that have now waited long enough to be judged and were not matched
    /// by an XInput press. Presses still inside the correlation window stay pending.
    /// </summary>
    public IReadOnlyList<RawGamepadButtonPress> Release(DateTime nowUtc)
    {
        _controllerPresses.RemoveAll(time => nowUtc - time > PendingLifetime);

        if (_pending.Count == 0)
        {
            return [];
        }

        List<RawGamepadButtonPress>? released = null;

        for (var index = _pending.Count - 1; index >= 0; index--)
        {
            var press = _pending[index];
            var age = nowUtc - press.ObservedUtc;

            if (age < CorrelationWindow && age >= TimeSpan.Zero)
            {
                continue;
            }

            _pending.RemoveAt(index);

            if (age <= PendingLifetime && !HasControllerPressNear(press.ObservedUtc))
            {
                released ??= [];
                released.Add(press);
            }
        }

        if (released is null)
        {
            return [];
        }

        released.Reverse();
        return released;
    }

    private bool HasControllerPressNear(DateTime observedUtc) =>
        _controllerPresses.Any(time =>
        {
            var difference = time - observedUtc;
            return difference.Duration() <= CorrelationWindow;
        });
}
