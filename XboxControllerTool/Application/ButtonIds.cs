using XboxControllerTool.Core;

namespace XboxControllerTool.Application;

/// <summary>
/// Stable text identities for remappable buttons. XInput buttons are identified by their mask and
/// HID buttons by the controller's vendor/product pair plus the HID usage number, so an identity
/// survives both a reconnect and a move to a different controller slot.
/// </summary>
public static class ButtonIds
{
    private const string XInputPrefix = "xinput:";
    private const string HidPrefix = "hid:";

    public static string ForXInput(ushort mask) => XInputPrefix + mask.ToString("X4");

    public static string ForHid(string deviceId, ushort usage) => $"{HidPrefix}{deviceId}:{usage}";

    public static bool IsHid(string id) => id.StartsWith(HidPrefix, StringComparison.Ordinal);

    public static bool TryParseXInput(string id, out ushort mask)
    {
        mask = 0;

        return id.StartsWith(XInputPrefix, StringComparison.Ordinal) &&
               ushort.TryParse(id.AsSpan(XInputPrefix.Length), System.Globalization.NumberStyles.HexNumber, null, out mask);
    }

    public static bool TryParseHidUsage(string id, out ushort usage)
    {
        usage = 0;

        if (!IsHid(id))
        {
            return false;
        }

        var separator = id.LastIndexOf(':');

        return separator > HidPrefix.Length - 1 && ushort.TryParse(id.AsSpan(separator + 1), out usage);
    }

    public static bool IsWellFormed(string id) => TryParseXInput(id, out _) || TryParseHidUsage(id, out _);

    public static string LabelFor(string id)
    {
        if (TryParseXInput(id, out var mask))
        {
            return mask switch
            {
                (ushort)GamepadButton.RightThumb => "R3",
                (ushort)GamepadButton.Guide => "GUIDE",
                (ushort)GamepadButton.Extra => "EXTRA",
                _ => $"BUTTON {mask:X4}"
            };
        }

        return TryParseHidUsage(id, out var usage) ? $"HID {usage}" : "UNKNOWN";
    }
}
