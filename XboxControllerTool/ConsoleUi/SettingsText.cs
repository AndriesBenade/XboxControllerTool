using XboxControllerTool.Configuration;

namespace XboxControllerTool.ConsoleUi;

public static class SettingsText
{
    public static string Position(NotificationPosition position) => position switch
    {
        NotificationPosition.TopLeft => "TOP LEFT",
        NotificationPosition.TopCenter => "TOP CENTER",
        NotificationPosition.BottomLeft => "BOTTOM LEFT",
        NotificationPosition.BottomRight => "BOTTOM RIGHT",
        _ => "TOP RIGHT"
    };
}
