using System.Drawing;

namespace XboxControllerTool.ConsoleUi;

public sealed record Theme(
    string Name,
    ConsoleColor Background,
    ConsoleColor Title,
    ConsoleColor BandFrame,
    ConsoleColor PanelFrame,
    ConsoleColor PanelTitle,
    ConsoleColor Label,
    ConsoleColor Text,
    ConsoleColor Value,
    ConsoleColor Focus,
    ConsoleColor ButtonFrame,
    ConsoleColor ButtonLabel,
    ConsoleColor Positive,
    ConsoleColor Active,
    ConsoleColor Negative,
    Color NotificationBackground,
    Color NotificationAccent,
    Color NotificationTitle,
    Color NotificationSubtitle);
