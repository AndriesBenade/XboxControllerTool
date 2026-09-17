using System.Runtime.InteropServices;
using XboxControllerTool.Configuration;
using XboxControllerTool.ConsoleUi;

namespace XboxControllerTool.Windows;

public sealed class ConsoleFontController
{
    private static readonly int MinimumColumns = UiDimensions.MinimumColumns;
    private static readonly int MinimumRows = UiDimensions.MinimumRows;

    // Console font APIs are honoured by the classic console host only. Windows Terminal renders with
    // its own profile font and silently ignores them, so the size is reported as unsupported there.
    public bool IsSupported { get; private set; } = Environment.GetEnvironmentVariable("WT_SESSION") is null;

    public static short HeightFor(ConsoleFontSize size) => size switch
    {
        ConsoleFontSize.Small => 16,
        ConsoleFontSize.Large => 28,
        ConsoleFontSize.ExtraLarge => 36,
        _ => 20
    };

    public bool TryApply(ConsoleFontSize size)
    {
        var handle = NativeConsoleFont.GetStdHandle(NativeConsoleFont.StdOutputHandle);
        if (handle == nint.Zero || handle == new nint(-1))
        {
            IsSupported = false;
            return false;
        }

        var current = new ConsoleFontInfoEx { cbSize = (uint)Marshal.SizeOf<ConsoleFontInfoEx>() };
        if (!NativeConsoleFont.GetCurrentConsoleFontEx(handle, false, ref current))
        {
            IsSupported = false;
            return false;
        }

        var previousHeight = current.dwFontSizeY;
        var previousFace = current.FaceName;

        if (!TrySetHeight(handle, HeightFor(size)))
        {
            IsSupported = false;
            return false;
        }

        if (ConsoleFitsInterface())
        {
            return true;
        }

        TrySetHeight(handle, previousHeight, previousFace);
        return false;
    }

    private static bool TrySetHeight(nint handle, short height, string? faceName = null)
    {
        var font = new ConsoleFontInfoEx
        {
            cbSize = (uint)Marshal.SizeOf<ConsoleFontInfoEx>(),
            nFont = 0,
            dwFontSizeX = 0,
            dwFontSizeY = height,
            FontFamily = NativeConsoleFont.FixedPitchTrueType,
            FontWeight = NativeConsoleFont.NormalWeight,
            FaceName = string.IsNullOrWhiteSpace(faceName) ? "Consolas" : faceName
        };

        return NativeConsoleFont.SetCurrentConsoleFontEx(handle, false, ref font);
    }

    private static bool ConsoleFitsInterface()
    {
        try
        {
            return Console.LargestWindowWidth >= MinimumColumns && Console.LargestWindowHeight >= MinimumRows;
        }
        catch (IOException)
        {
            return false;
        }
    }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct ConsoleFontInfoEx
{
    public uint cbSize;
    public uint nFont;
    public short dwFontSizeX;
    public short dwFontSizeY;
    public int FontFamily;
    public int FontWeight;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public string FaceName;
}

internal static class NativeConsoleFont
{
    internal const int StdOutputHandle = -11;
    internal const int FixedPitchTrueType = 54;
    internal const int NormalWeight = 400;

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern nint GetStdHandle(int nStdHandle);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool GetCurrentConsoleFontEx(nint consoleOutput, [MarshalAs(UnmanagedType.Bool)] bool maximumWindow, ref ConsoleFontInfoEx fontInfo);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetCurrentConsoleFontEx(nint consoleOutput, [MarshalAs(UnmanagedType.Bool)] bool maximumWindow, ref ConsoleFontInfoEx fontInfo);
}
