namespace XboxControllerTool.ConsoleUi;

public static class ControllerButton
{
    public const string A = "A";
    public const string B = "B";
    public const string X = "X";
    public const string Y = "Y";
    public const string LeftBumper = "LB";
    public const string RightBumper = "RB";
    public const string LeftTrigger = "LT";
    public const string RightTrigger = "RT";
    public const string Back = "BACK";
    public const string Start = "START";
    public const string LeftStick = "L-STICK";
    public const string RightStick = "R-STICK";
    public const string Up = "UP";
    public const string Down = "DOWN";
    public const string UpDown = "UP/DN";
    public const string LeftRight = "L/R";

    public static ConsoleSegment[] Render(string label) =>
    [
        new ConsoleSegment("[ ", ConsoleTheme.ButtonFrame),
        new ConsoleSegment(label, ConsoleTheme.ButtonLabel),
        new ConsoleSegment(" ]", ConsoleTheme.ButtonFrame)
    ];

    public static ConsoleSegment[] Cell(string label, string action, int badgeWidth, int actionWidth)
    {
        var badgePad = Math.Max(badgeWidth - (label.Length + 4), 1);

        return
        [
            new ConsoleSegment("[ ", ConsoleTheme.ButtonFrame),
            new ConsoleSegment(label, ConsoleTheme.ButtonLabel),
            new ConsoleSegment(" ]" + new string(' ', badgePad), ConsoleTheme.ButtonFrame),
            new ConsoleSegment(action.PadRight(actionWidth), ConsoleTheme.Text)
        ];
    }

    public static ConsoleSegment[] Badge(string label, int width)
    {
        var pad = Math.Max(width - (label.Length + 4), 0);

        return
        [
            new ConsoleSegment("[ ", ConsoleTheme.ButtonFrame),
            new ConsoleSegment(label, ConsoleTheme.ButtonLabel),
            new ConsoleSegment(" ]" + new string(' ', pad), ConsoleTheme.ButtonFrame)
        ];
    }

    public static ConsoleSegment[] BadgeSpacer(int width) => [new ConsoleSegment(new string(' ', width))];
}
