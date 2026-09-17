namespace XboxControllerTool.Windows;

internal static class ForegroundWindowActivator
{
    public static void Activate(nint targetWindow)
    {
        if (NativeWindowMethods.IsIconic(targetWindow))
        {
            NativeWindowMethods.ShowWindow(targetWindow, NativeWindowMethods.SwRestore);
        }

        var foregroundWindow = NativeWindowMethods.GetForegroundWindow();
        if (foregroundWindow == targetWindow)
        {
            return;
        }

        var foregroundThreadId = NativeWindowMethods.GetWindowThreadProcessId(foregroundWindow, out _);
        var currentThreadId = NativeWindowMethods.GetCurrentThreadId();
        var attached = foregroundThreadId != currentThreadId &&
                        NativeWindowMethods.AttachThreadInput(currentThreadId, foregroundThreadId, true);

        try
        {
            NativeWindowMethods.BringWindowToTop(targetWindow);
            NativeWindowMethods.SetForegroundWindow(targetWindow);
        }
        finally
        {
            if (attached)
            {
                NativeWindowMethods.AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
        }
    }
}
