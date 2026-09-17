using XboxControllerTool.Diagnostics;

namespace XboxControllerTool.Tests;

public class ErrorReporterTests : IDisposable
{
    private readonly string _logPath = Path.Combine(Path.GetTempPath(), $"xct-errors-{Guid.NewGuid()}.log");

    public void Dispose()
    {
        if (File.Exists(_logPath))
        {
            File.Delete(_logPath);
        }
    }

    [Fact]
    public void AReportedErrorIsCountedAndWrittenToTheLog()
    {
        var reporter = new ErrorReporter(_logPath);

        reporter.Report("input loop", new InvalidOperationException("stick fell off"));

        Assert.Equal(1, reporter.ErrorCount);
        Assert.Contains("stick fell off", File.ReadAllText(_logPath));
        Assert.Contains("input loop", File.ReadAllText(_logPath));
    }

    [Fact]
    public void EveryErrorIsAppendedRatherThanReplacingTheLast()
    {
        var reporter = new ErrorReporter(_logPath);

        reporter.Report("first", new InvalidOperationException("one"));
        reporter.Report("second", new IOException("two"));

        var log = File.ReadAllText(_logPath);

        Assert.Equal(2, reporter.ErrorCount);
        Assert.Contains("one", log);
        Assert.Contains("two", log);
    }

    [Fact]
    public void AFatalErrorRecordsTheLogLocationSoItCanBeFoundAfterTheWindowCloses()
    {
        var reporter = new ErrorReporter(_logPath);

        reporter.ReportFatal("the application stopped", new InvalidOperationException("boom"));

        Assert.Contains("FATAL", File.ReadAllText(_logPath));
        Assert.Equal(_logPath, reporter.LogPath);
    }

    [Fact]
    public void AnUnwritableLogNeverTurnsErrorReportingIntoASecondFailure()
    {
        var reporter = new ErrorReporter(Path.Combine(Path.GetTempPath(), $"xct-missing-{Guid.NewGuid()}", "nested", "errors.log"));

        reporter.Report("input loop", new InvalidOperationException("boom"));

        Assert.Equal(1, reporter.ErrorCount);
    }

    [Fact]
    public void AnErrorIsDescribedByTypeAndMessageSoTheToastIsReadable()
    {
        Assert.Equal(
            "InvalidOperationException: stick fell off",
            ErrorReporter.Describe(new InvalidOperationException("stick fell off")));
    }

    [Fact]
    public void AnErrorWithNoMessageStillDescribesItself()
    {
        Assert.Equal("CustomFailure", ErrorReporter.Describe(new CustomFailure(string.Empty)));
    }

    private sealed class CustomFailure(string message) : Exception(message);
}
