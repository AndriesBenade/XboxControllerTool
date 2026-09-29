using XboxControllerTool.Windows;

namespace XboxControllerTool.Tests;

/// <summary>
/// The keyboard and mouse path cannot be driven from a test - it needs a real console with a real
/// person clicking in it. What is checked here is that it degrades quietly when there is no console
/// to attach to, which is exactly the situation in this test host.
/// </summary>
public class ConsoleInputSourceTests
{
    [Fact]
    public void AttachingWithoutAConsoleNeverThrows()
    {
        using var source = new ConsoleInputSource();

        Assert.Empty(source.Drain());
    }

    [Fact]
    public void DrainingRepeatedlyStaysSafe()
    {
        using var source = new ConsoleInputSource();

        for (var attempt = 0; attempt < 50; attempt++)
        {
            Assert.Empty(source.Drain());
        }
    }

    [Fact]
    public void DisposingRestoresTheConsoleWithoutComplaint()
    {
        var source = new ConsoleInputSource();

        source.Dispose();
        source.Dispose();
    }

    [Fact]
    public void DrainingAfterDisposalIsHarmless()
    {
        var source = new ConsoleInputSource();
        source.Dispose();

        Assert.Empty(source.Drain());
    }
}
