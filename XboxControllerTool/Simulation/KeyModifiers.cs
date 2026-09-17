namespace XboxControllerTool.Simulation;

[Flags]
public enum KeyModifiers
{
    None = 0,
    Control = 1,
    Alt = 2,
    Shift = 4,
    Windows = 8
}

public static class KeyModifiersFormatting
{
    public static string Describe(KeyModifiers modifiers, string keyName)
    {
        var parts = new List<string>(5);

        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }

        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            parts.Add("Alt");
        }

        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            parts.Add("Shift");
        }

        if (modifiers.HasFlag(KeyModifiers.Windows))
        {
            parts.Add("Win");
        }

        parts.Add(keyName);
        return string.Join(" + ", parts);
    }
}
