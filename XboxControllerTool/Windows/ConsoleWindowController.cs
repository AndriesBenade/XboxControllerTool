using System.Drawing;
using System.Windows.Forms;
using XboxControllerTool.ConsoleUi;

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

    /// <summary>
    /// Resizes the console to exactly what the interface needs at the current font size, clamped to
    /// whatever Windows will allow for that font. Shrinking needs the window reduced before the
    /// buffer, growing needs the buffer enlarged before the window, so both orders are applied.
    /// </summary>
    public void ApplyPreferredSize()
    {
        try
        {
            var columns = Fit(UiDimensions.RequiredColumns, Console.LargestWindowWidth, 40);
            var rows = Fit(UiDimensions.PreferredRows, Console.LargestWindowHeight, UiDimensions.MinimumRows);

            TryResize(() => Console.SetWindowSize(Math.Min(columns, Console.WindowWidth), Math.Min(rows, Console.WindowHeight)));
            TryResize(() => Console.SetBufferSize(columns, rows));
            TryResize(() => Console.SetWindowSize(columns, rows));
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or IOException or PlatformNotSupportedException)
        {
        }
    }

    private static int Fit(int desired, int available, int floor) =>
        available <= 0 ? Math.Max(desired, floor) : Math.Max(Math.Min(desired, available), Math.Min(floor, available));

    private static void TryResize(Action resize)
    {
        try
        {
            resize();
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or IOException or PlatformNotSupportedException)
        {
        }
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
