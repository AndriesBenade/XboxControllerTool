using XboxControllerTool.ConsoleUi;

namespace XboxControllerTool.Tests;

/// <summary>
/// Regression cover for a crash where a screen taller than the console buffer threw
/// <see cref="ArgumentOutOfRangeException"/> out of <c>SetCursorPosition</c> and killed the app on
/// startup. Rendering must clip instead, because the user can resize the console at any time.
/// </summary>
[Collection(nameof(ConsoleFrameRendererTests))]
public class ConsoleFrameRendererTests
{
    [Fact]
    public void AScreenTallerThanTheConsoleBufferIsClippedInsteadOfThrowing()
    {
        var renderer = new ConsoleFrameRenderer();

        WithConsoleSize(width: 80, height: 10, () =>
        {
            renderer.Render(Screen(40));
            renderer.Render(Screen(40));
        });
    }

    [Fact]
    public void AScreenThatGrowsPastTheBufferBetweenFramesIsStillSafe()
    {
        var renderer = new ConsoleFrameRenderer();

        WithConsoleSize(width: 80, height: 12, () =>
        {
            renderer.Render(Screen(8));
            renderer.Render(Screen(60));
            renderer.Invalidate();
            renderer.Render(Screen(3));
        });
    }

    [Fact]
    public void RenderingNothingIsSafe()
    {
        var renderer = new ConsoleFrameRenderer();

        WithConsoleSize(width: 80, height: 10, () => renderer.Render([]));
    }

    private static IReadOnlyList<ConsoleLine> Screen(int rows) =>
        [.. Enumerable.Range(0, rows).Select(row => new ConsoleLine([new ConsoleSegment($"row {row}")]))];

    /// <summary>
    /// Shrinks the real console around the assertion, restoring it afterwards. If this test host has
    /// no resizable console the body still runs, which exercises the renderer's fallback path.
    /// </summary>
    private static void WithConsoleSize(int width, int height, Action body)
    {
        int originalWidth;
        int originalHeight;
        int originalBufferWidth;
        int originalBufferHeight;

        try
        {
            originalWidth = Console.WindowWidth;
            originalHeight = Console.WindowHeight;
            originalBufferWidth = Console.BufferWidth;
            originalBufferHeight = Console.BufferHeight;
        }
        catch (Exception ex) when (ex is IOException or PlatformNotSupportedException)
        {
            body();
            return;
        }

        try
        {
            TryResize(() => Console.SetWindowSize(Math.Min(width, originalWidth), Math.Min(height, originalHeight)));
            TryResize(() => Console.SetBufferSize(width, height));
            body();
        }
        finally
        {
            TryResize(() => Console.SetBufferSize(originalBufferWidth, originalBufferHeight));
            TryResize(() => Console.SetWindowSize(originalWidth, originalHeight));
            TryResize(Console.Clear);
        }
    }

    private static void TryResize(Action resize)
    {
        try
        {
            resize();
        }
        catch (Exception ex) when (ex is IOException or ArgumentOutOfRangeException or PlatformNotSupportedException)
        {
        }
    }
}
