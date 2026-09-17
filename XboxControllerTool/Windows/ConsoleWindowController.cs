using System.Drawing;
using System.Windows.Forms;

namespace XboxControllerTool.Windows;

public sealed class ConsoleWindowController
{
    private nint _previousForegroundWindow;

    public nint Handle { get; } = NativeWindowMethods.GetConsoleWindow();

    public bool IsTopMost { get; private set; }

    public bool IsMinimized => NativeWindowMethods.IsIconic(Handle);

    public void Minimize()
    {
        NativeWindowMethods.ShowWindow(Handle, NativeWindowMethods.SwMinimize);
    }

    public void BringToForegroundAndTopMost()
    {
        if (NativeWindowMethods.IsIconic(Handle))
        {
            NativeWindowMethods.ShowWindow(Handle, NativeWindowMethods.SwRestore);
        }

        _previousForegroundWindow = NativeWindowMethods.GetForegroundWindow();

        CenterOnScreen();
        ForegroundWindowActivator.Activate(Handle);

        NativeWindowMethods.SetWindowPos(
            Handle,
            NativeWindowMethods.HwndTopMost,
            0, 0, 0, 0,
            NativeWindowMethods.SwpNoMove | NativeWindowMethods.SwpNoSize | NativeWindowMethods.SwpShowWindow);

        IsTopMost = true;
    }

    public void Hide(bool restorePreviousForeground)
    {
        NativeWindowMethods.SetWindowPos(
            Handle,
            NativeWindowMethods.HwndNoTopMost,
            0, 0, 0, 0,
            NativeWindowMethods.SwpNoMove | NativeWindowMethods.SwpNoSize);

        IsTopMost = false;

        if (restorePreviousForeground)
        {
            RestorePreviousForeground();
        }

        Minimize();
    }

    public void CenterOnScreen()
    {
        if (!NativeWindowMethods.GetWindowRect(Handle, out var rect))
        {
            return;
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        var workingArea = Screen.FromHandle(Handle).WorkingArea;

        var left = workingArea.Left + Math.Max((workingArea.Width - width) / 2, 0);
        var top = workingArea.Top + Math.Max((workingArea.Height - height) / 2, 0);

        NativeWindowMethods.SetWindowPos(Handle, nint.Zero, left, top, 0, 0, NativeWindowMethods.SwpNoSize | NativeWindowMethods.SwpNoZOrder);
    }

    private void RestorePreviousForeground()
    {
        var target = _previousForegroundWindow;

        if (target == nint.Zero || target == Handle || !NativeWindowMethods.IsWindow(target))
        {
            return;
        }

        ForegroundWindowActivator.Activate(target);
    }
}
