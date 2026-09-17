namespace XboxControllerTool.Simulation;

public readonly record struct AssignableKey(string Name, ushort VirtualKey, string Group);

public static class KeyCatalog
{
    public const string Letters = "LETTERS";
    public const string Digits = "DIGITS";
    public const string Function = "FUNCTION";
    public const string Navigation = "NAVIGATION";
    public const string Editing = "EDITING";
    public const string Media = "MEDIA";

    public static readonly IReadOnlyList<string> Groups = [Letters, Digits, Function, Navigation, Editing, Media];

    public static readonly IReadOnlyList<AssignableKey> All = BuildCatalog();

    public static IReadOnlyList<AssignableKey> InGroup(string group) =>
        [.. All.Where(key => key.Group == group)];

    public static AssignableKey? Find(string name) =>
        All.Cast<AssignableKey?>().FirstOrDefault(key => key!.Value.Name == name);

    public static bool IsKnown(string name) => Find(name) is not null;

    private static List<AssignableKey> BuildCatalog()
    {
        var keys = new List<AssignableKey>();

        for (var letter = 'A'; letter <= 'Z'; letter++)
        {
            keys.Add(new AssignableKey(letter.ToString(), (ushort)letter, Letters));
        }

        for (var digit = 0; digit <= 9; digit++)
        {
            keys.Add(new AssignableKey(digit.ToString(), (ushort)(0x30 + digit), Digits));
        }

        for (var index = 1; index <= 12; index++)
        {
            keys.Add(new AssignableKey($"F{index}", (ushort)(0x6F + index), Function));
        }

        keys.AddRange(
        [
            new AssignableKey("Left", 0x25, Navigation),
            new AssignableKey("Up", 0x26, Navigation),
            new AssignableKey("Right", 0x27, Navigation),
            new AssignableKey("Down", 0x28, Navigation),
            new AssignableKey("Home", 0x24, Navigation),
            new AssignableKey("End", 0x23, Navigation),
            new AssignableKey("PageUp", 0x21, Navigation),
            new AssignableKey("PageDown", 0x22, Navigation),
            new AssignableKey("Tab", 0x09, Navigation),

            new AssignableKey("Enter", 0x0D, Editing),
            new AssignableKey("Esc", 0x1B, Editing),
            new AssignableKey("Space", 0x20, Editing),
            new AssignableKey("Backspace", 0x08, Editing),
            new AssignableKey("Delete", 0x2E, Editing),
            new AssignableKey("Insert", 0x2D, Editing),
            new AssignableKey("PrintScreen", 0x2C, Editing),

            new AssignableKey("VolumeUp", 0xAF, Media),
            new AssignableKey("VolumeDown", 0xAE, Media),
            new AssignableKey("VolumeMute", 0xAD, Media),
            new AssignableKey("PlayPause", 0xB3, Media),
            new AssignableKey("NextTrack", 0xB0, Media),
            new AssignableKey("PrevTrack", 0xB1, Media)
        ]);

        return keys;
    }
}
