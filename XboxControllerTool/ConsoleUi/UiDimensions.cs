namespace XboxControllerTool.ConsoleUi;

/// <summary>
/// The console window size the interface actually needs, derived from the panel grid rather than
/// from arbitrary numbers, so changing the layout changes the window that gets requested.
/// </summary>
public static class UiDimensions
{
    private const int SideMargin = 2;

    /// <summary>One spare column so the renderer, which writes to <c>WindowWidth - 1</c>, still covers a full panel row.</summary>
    private const int RendererSpareColumn = 1;

    /// <summary>
    /// Tallest screen (home with its custom-mapping rows filled) plus one spare row.
    /// <c>ScreenHeightTests</c> fails the build if any screen outgrows this, because a screen taller
    /// than the console buffer used to be sized from it would be clipped.
    /// </summary>
    private const int TallestScreenRows = 39;

    public const int RequiredColumns = Panel.Width + SideMargin + RendererSpareColumn;
    public const int PreferredRows = TallestScreenRows;

    /// <summary>Below this the compact layout still renders every screen without clipping.</summary>
    public const int MinimumRows = 24;

    public const int MinimumColumns = Panel.Width + RendererSpareColumn;
}
