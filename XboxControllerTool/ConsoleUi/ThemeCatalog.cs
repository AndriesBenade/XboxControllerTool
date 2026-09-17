using System.Drawing;

namespace XboxControllerTool.ConsoleUi;

public static class ThemeCatalog
{
    public static readonly Theme Dark = new(
        "Dark",
        Background: ConsoleColor.Black,
        Title: ConsoleColor.Cyan,
        BandFrame: ConsoleColor.DarkCyan,
        PanelFrame: ConsoleColor.DarkGray,
        PanelTitle: ConsoleColor.DarkCyan,
        Label: ConsoleColor.DarkGray,
        Text: ConsoleColor.Gray,
        Value: ConsoleColor.White,
        Focus: ConsoleColor.Cyan,
        ButtonFrame: ConsoleColor.DarkGray,
        ButtonLabel: ConsoleColor.White,
        Positive: ConsoleColor.Green,
        Active: ConsoleColor.Yellow,
        Negative: ConsoleColor.Red,
        NotificationBackground: Color.FromArgb(16, 18, 22),
        NotificationAccent: Color.FromArgb(0, 188, 212),
        NotificationTitle: Color.White,
        NotificationSubtitle: Color.FromArgb(150, 155, 165));

    public static readonly Theme Light = new(
        "Light",
        Background: ConsoleColor.White,
        Title: ConsoleColor.DarkBlue,
        BandFrame: ConsoleColor.DarkBlue,
        PanelFrame: ConsoleColor.DarkGray,
        PanelTitle: ConsoleColor.DarkBlue,
        Label: ConsoleColor.DarkGray,
        Text: ConsoleColor.Black,
        Value: ConsoleColor.Black,
        Focus: ConsoleColor.DarkMagenta,
        ButtonFrame: ConsoleColor.DarkGray,
        ButtonLabel: ConsoleColor.Black,
        Positive: ConsoleColor.DarkGreen,
        Active: ConsoleColor.DarkYellow,
        Negative: ConsoleColor.DarkRed,
        NotificationBackground: Color.FromArgb(246, 247, 250),
        NotificationAccent: Color.FromArgb(40, 90, 200),
        NotificationTitle: Color.FromArgb(20, 22, 28),
        NotificationSubtitle: Color.FromArgb(90, 96, 110));

    public static readonly Theme Xbox = new(
        "Xbox",
        Background: ConsoleColor.Black,
        Title: ConsoleColor.Green,
        BandFrame: ConsoleColor.DarkGreen,
        PanelFrame: ConsoleColor.DarkGray,
        PanelTitle: ConsoleColor.DarkGreen,
        Label: ConsoleColor.DarkGray,
        Text: ConsoleColor.Gray,
        Value: ConsoleColor.White,
        Focus: ConsoleColor.Green,
        ButtonFrame: ConsoleColor.DarkGray,
        ButtonLabel: ConsoleColor.White,
        Positive: ConsoleColor.Green,
        Active: ConsoleColor.Yellow,
        Negative: ConsoleColor.Red,
        NotificationBackground: Color.FromArgb(14, 20, 14),
        NotificationAccent: Color.FromArgb(16, 185, 64),
        NotificationTitle: Color.White,
        NotificationSubtitle: Color.FromArgb(150, 165, 150));

    public static readonly Theme Girly = new(
        "Girly",
        Background: ConsoleColor.Black,
        Title: ConsoleColor.Magenta,
        BandFrame: ConsoleColor.DarkMagenta,
        PanelFrame: ConsoleColor.DarkGray,
        PanelTitle: ConsoleColor.Magenta,
        Label: ConsoleColor.DarkGray,
        Text: ConsoleColor.Gray,
        Value: ConsoleColor.White,
        Focus: ConsoleColor.Magenta,
        ButtonFrame: ConsoleColor.DarkMagenta,
        ButtonLabel: ConsoleColor.White,
        Positive: ConsoleColor.Green,
        Active: ConsoleColor.Magenta,
        Negative: ConsoleColor.Red,
        NotificationBackground: Color.FromArgb(26, 14, 24),
        NotificationAccent: Color.FromArgb(236, 72, 153),
        NotificationTitle: Color.White,
        NotificationSubtitle: Color.FromArgb(196, 150, 185));

    public static readonly Theme Matrix = new(
        "Matrix",
        Background: ConsoleColor.Black,
        Title: ConsoleColor.Green,
        BandFrame: ConsoleColor.DarkGreen,
        PanelFrame: ConsoleColor.DarkGreen,
        PanelTitle: ConsoleColor.Green,
        Label: ConsoleColor.DarkGreen,
        Text: ConsoleColor.Green,
        Value: ConsoleColor.Green,
        Focus: ConsoleColor.White,
        ButtonFrame: ConsoleColor.DarkGreen,
        ButtonLabel: ConsoleColor.Green,
        Positive: ConsoleColor.Green,
        Active: ConsoleColor.White,
        Negative: ConsoleColor.Red,
        NotificationBackground: Color.FromArgb(6, 14, 6),
        NotificationAccent: Color.FromArgb(0, 255, 90),
        NotificationTitle: Color.FromArgb(120, 255, 150),
        NotificationSubtitle: Color.FromArgb(60, 170, 90));

    public static readonly Theme Ocean = new(
        "Ocean",
        Background: ConsoleColor.Black,
        Title: ConsoleColor.Blue,
        BandFrame: ConsoleColor.DarkBlue,
        PanelFrame: ConsoleColor.DarkBlue,
        PanelTitle: ConsoleColor.Blue,
        Label: ConsoleColor.DarkGray,
        Text: ConsoleColor.Gray,
        Value: ConsoleColor.White,
        Focus: ConsoleColor.Cyan,
        ButtonFrame: ConsoleColor.DarkBlue,
        ButtonLabel: ConsoleColor.White,
        Positive: ConsoleColor.Green,
        Active: ConsoleColor.Cyan,
        Negative: ConsoleColor.Red,
        NotificationBackground: Color.FromArgb(10, 18, 30),
        NotificationAccent: Color.FromArgb(56, 140, 255),
        NotificationTitle: Color.White,
        NotificationSubtitle: Color.FromArgb(140, 165, 200));

    public static readonly Theme Amber = new(
        "Amber",
        Background: ConsoleColor.Black,
        Title: ConsoleColor.Yellow,
        BandFrame: ConsoleColor.DarkYellow,
        PanelFrame: ConsoleColor.DarkYellow,
        PanelTitle: ConsoleColor.Yellow,
        Label: ConsoleColor.DarkYellow,
        Text: ConsoleColor.DarkYellow,
        Value: ConsoleColor.Yellow,
        Focus: ConsoleColor.White,
        ButtonFrame: ConsoleColor.DarkYellow,
        ButtonLabel: ConsoleColor.Yellow,
        Positive: ConsoleColor.Green,
        Active: ConsoleColor.White,
        Negative: ConsoleColor.Red,
        NotificationBackground: Color.FromArgb(24, 16, 4),
        NotificationAccent: Color.FromArgb(255, 176, 0),
        NotificationTitle: Color.FromArgb(255, 214, 120),
        NotificationSubtitle: Color.FromArgb(180, 130, 40));

    public static readonly IReadOnlyList<Theme> All = [Dark, Light, Xbox, Girly, Matrix, Ocean, Amber];

    public static Theme Resolve(string? name) =>
        All.FirstOrDefault(theme => string.Equals(theme.Name, name, StringComparison.OrdinalIgnoreCase)) ?? Dark;

    public static Theme Next(Theme current, int direction)
    {
        var index = All.ToList().IndexOf(current);
        var next = (index + direction + All.Count) % All.Count;
        return All[next];
    }
}
