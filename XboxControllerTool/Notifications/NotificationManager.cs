using XboxControllerTool.Configuration;

namespace XboxControllerTool.Notifications;

public sealed class NotificationManager : INotificationService, IDisposable
{
    private readonly NotificationOverlayHost _host;
    private readonly AppSettings _settings;

    public NotificationManager(AppSettings settings)
    {
        _settings = settings;
        _host = new NotificationOverlayHost();
    }

    public void ShowTransient(string title, string? subtitle = null, NotificationKind kind = NotificationKind.Info)
    {
        _host.Display(title, subtitle, kind, _settings.NotificationDurationMs, _settings.NotificationPosition);
    }

    public void Dispose() => _host.Dispose();
}
