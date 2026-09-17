namespace XboxControllerTool.Windows.RawInput;

/// <summary>
/// Supplies gamepad button presses seen through raw HID reports rather than XInput.
/// </summary>
public interface IRawGamepadSource
{
    bool IsRunning { get; }

    bool TryDequeue(out RawGamepadButtonPress press);

    IReadOnlyList<RawGamepadDevice> DescribeDevices();
}
