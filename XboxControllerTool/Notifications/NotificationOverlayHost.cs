using System.Windows.Forms;
using XboxControllerTool.Configuration;

namespace XboxControllerTool.Notifications;

internal sealed class NotificationOverlayHost : IDisposable
{
    private readonly Thread _thread;
    private readonly ManualResetEventSlim _ready = new(false);
    private NotificationOverlayForm? _form;

    public NotificationOverlayHost()
    {
        _thread = new Thread(RunMessageLoop) { IsBackground = true };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        _ready.Wait();
    }

    private void RunMessageLoop()
    {
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
        _form = new NotificationOverlayForm();
        _ = _form.Handle;
        _ready.Set();
        System.Windows.Forms.Application.Run();
    }

    public void Display(string title, string? subtitle, NotificationKind kind, int durationMs, NotificationPosition position)
    {
        var form = _form;
        if (form is null || form.IsDisposed)
        {
            return;
        }

        try
        {
            form.BeginInvoke(() => form.DisplayMessage(title, subtitle, kind, durationMs, position));
        }
        catch (ObjectDisposedException)
        {
        }
        catch (InvalidOperationException)
        {
        }
    }

    public void Dispose()
    {
        var form = _form;
        if (form is not null && !form.IsDisposed)
        {
            try
            {
                form.Invoke(() =>
                {
                    form.Dispose();
                    System.Windows.Forms.Application.ExitThread();
                });
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }

        _thread.Join(1000);
        _ready.Dispose();
    }
}
