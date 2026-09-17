namespace XboxControllerTool.Notifications;

public interface INotificationService
{
    void ShowTransient(string title, string? subtitle = null, NotificationKind kind = NotificationKind.Info);
}
