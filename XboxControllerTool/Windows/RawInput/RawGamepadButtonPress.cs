namespace XboxControllerTool.Windows.RawInput;

/// <summary>
/// A HID gamepad button that went from released to pressed, identified by the controller's
/// vendor/product ids and the HID usage number so the identity survives reconnects.
/// </summary>
public readonly record struct RawGamepadButtonPress(string DeviceId, ushort Usage, DateTime ObservedUtc);
