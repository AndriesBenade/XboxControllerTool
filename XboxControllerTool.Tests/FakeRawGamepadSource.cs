using XboxControllerTool.Windows.RawInput;

namespace XboxControllerTool.Tests;

public sealed class FakeRawGamepadSource : IRawGamepadSource
{
    private readonly Queue<RawGamepadButtonPress> _presses = new();

    public bool IsRunning { get; set; } = true;

    public List<RawGamepadDevice> Devices { get; } = [];

    public IReadOnlyList<RawGamepadDevice> DescribeDevices() => Devices;

    public void Enqueue(RawGamepadButtonPress press) => _presses.Enqueue(press);

    public bool TryDequeue(out RawGamepadButtonPress press)
    {
        if (_presses.Count == 0)
        {
            press = default;
            return false;
        }

        press = _presses.Dequeue();
        return true;
    }
}
