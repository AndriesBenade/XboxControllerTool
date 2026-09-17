using XboxControllerTool.Configuration;

namespace XboxControllerTool.Windows;

public sealed class WindowPlacementStore
{
    public void ApplySize(nint handle, WindowPlacementSettings settings)
    {
        if (!settings.HasValue || settings.Width <= 0 || settings.Height <= 0)
        {
            return;
        }

        NativeWindowMethods.SetWindowPos(
            handle,
            nint.Zero,
            0, 0, settings.Width, settings.Height,
            NativeWindowMethods.SwpNoMove | NativeWindowMethods.SwpNoZOrder);
    }

    public WindowPlacementSettings Capture(nint handle)
    {
        var placement = new NativeWindowPlacement
        {
            length = System.Runtime.InteropServices.Marshal.SizeOf<NativeWindowPlacement>()
        };

        if (!NativeWindowMethods.GetWindowPlacement(handle, ref placement))
        {
            return new WindowPlacementSettings { HasValue = false };
        }

        return new WindowPlacementSettings
        {
            HasValue = true,
            Width = Math.Max(placement.rcNormalPosition.Right - placement.rcNormalPosition.Left, 1),
            Height = Math.Max(placement.rcNormalPosition.Bottom - placement.rcNormalPosition.Top, 1)
        };
    }
}
