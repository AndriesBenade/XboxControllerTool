using System.Drawing;
using System.Windows.Forms;
using XboxControllerTool.Configuration;

namespace XboxControllerTool.Notifications;

internal sealed class NotificationOverlayForm : Form
{
    private const int WsExNoActivate = 0x08000000;
    private const int WsExToolWindow = 0x00000080;
    private const int WsExTopMost = 0x00000008;
    private const int NotificationMargin = 28;
    private const int ContentWidth = 360;

    private readonly Panel _accentBar;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly System.Windows.Forms.Timer _dismissTimer;

    public NotificationOverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.FromArgb(16, 18, 22);
        Opacity = 0.97;
        Width = ContentWidth;
        Padding = new Padding(0);

        _accentBar = new Panel { Dock = DockStyle.Left, Width = 8, BackColor = Color.FromArgb(0, 188, 212) };

        _titleLabel = new Label
        {
            AutoSize = false,
            ForeColor = Color.White,
            Font = new Font("Segoe UI Semibold", 14f, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(26, 14),
            Size = new Size(ContentWidth - 42, 30)
        };

        _subtitleLabel = new Label
        {
            AutoSize = false,
            ForeColor = Color.FromArgb(150, 155, 165),
            Font = new Font("Segoe UI", 10.5f, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(26, 46),
            Size = new Size(ContentWidth - 42, 24)
        };

        Controls.Add(_titleLabel);
        Controls.Add(_subtitleLabel);
        Controls.Add(_accentBar);

        _dismissTimer = new System.Windows.Forms.Timer();
        _dismissTimer.Tick += OnDismissTick;
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |= WsExNoActivate | WsExToolWindow | WsExTopMost;
            return parameters;
        }
    }

    public void DisplayMessage(string title, string? subtitle, NotificationKind kind, int durationMs, NotificationPosition position)
    {
        _titleLabel.Text = title;
        var hasSubtitle = !string.IsNullOrWhiteSpace(subtitle);
        _subtitleLabel.Visible = hasSubtitle;
        _subtitleLabel.Text = subtitle ?? string.Empty;
        _accentBar.BackColor = AccentColorFor(kind);

        Height = hasSubtitle ? 84 : 60;
        _titleLabel.Location = hasSubtitle ? new Point(26, 12) : new Point(26, (Height - 30) / 2);

        var workingArea = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = ComputeLocation(workingArea, position);

        _dismissTimer.Stop();
        _dismissTimer.Interval = Math.Max(durationMs, 200);

        if (!Visible)
        {
            Show();
        }

        _dismissTimer.Start();
    }

    private Point ComputeLocation(Rectangle workingArea, NotificationPosition position) => position switch
    {
        NotificationPosition.TopLeft => new Point(workingArea.Left + NotificationMargin, workingArea.Top + NotificationMargin),
        NotificationPosition.TopCenter => new Point(workingArea.Left + (workingArea.Width - Width) / 2, workingArea.Top + NotificationMargin),
        NotificationPosition.BottomLeft => new Point(workingArea.Left + NotificationMargin, workingArea.Bottom - Height - NotificationMargin),
        NotificationPosition.BottomRight => new Point(workingArea.Right - Width - NotificationMargin, workingArea.Bottom - Height - NotificationMargin),
        _ => new Point(workingArea.Right - Width - NotificationMargin, workingArea.Top + NotificationMargin)
    };

    private static Color AccentColorFor(NotificationKind kind) => kind switch
    {
        NotificationKind.Success => Color.FromArgb(76, 200, 120),
        NotificationKind.Warning => Color.FromArgb(230, 170, 60),
        _ => Color.FromArgb(0, 188, 212)
    };

    private void OnDismissTick(object? sender, EventArgs e)
    {
        _dismissTimer.Stop();
        Hide();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dismissTimer.Tick -= OnDismissTick;
            _dismissTimer.Dispose();
        }

        base.Dispose(disposing);
    }
}
