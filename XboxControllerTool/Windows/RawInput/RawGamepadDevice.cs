namespace XboxControllerTool.Windows.RawInput;

/// <summary>
/// A gamepad as Windows itself describes it. <paramref name="ButtonCount"/> is the number of
/// buttons the controller's own HID report descriptor declares, which is the hard ceiling on what
/// any application can ever detect from it.
/// </summary>
public readonly record struct RawGamepadDevice(string DeviceId, int ButtonCount);
