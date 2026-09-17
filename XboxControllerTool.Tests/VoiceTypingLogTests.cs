using XboxControllerTool.Windows;

namespace XboxControllerTool.Tests;

public class VoiceTypingLogTests : IDisposable
{
    private readonly string _logPath = Path.Combine(Path.GetTempPath(), $"xct-voice-{Guid.NewGuid()}.log");

    public void Dispose()
    {
        if (File.Exists(_logPath))
        {
            File.Delete(_logPath);
        }
    }

    [Fact]
    public void ASnapshotRecordsEveryRowUnderItsHeading()
    {
        var log = new VoiceTypingLog(_logPath);

        log.Begin();
        log.Snapshot("on screen", ["window one", "window two"]);
        log.Line("done");

        var contents = File.ReadAllText(_logPath);

        Assert.Contains("voice typing switched off", contents);
        Assert.Contains("on screen (2)", contents);
        Assert.Contains("window one", contents);
        Assert.Contains("window two", contents);
        Assert.Contains("done", contents);
    }

    [Fact]
    public void AnUnwritableLogNeverThrows()
    {
        var log = new VoiceTypingLog(Path.Combine(Path.GetTempPath(), $"xct-missing-{Guid.NewGuid()}", "nested", "voice.log"));

        log.Begin();
        log.Snapshot("on screen", ["anything"]);
    }

    [Fact]
    public void ALogWithNoPathIsSilentlyInert()
    {
        var log = new VoiceTypingLog(null);

        log.Begin();
        log.Line("nothing should happen");

        Assert.Null(log.FilePath);
    }

    [Fact]
    public void ClosingRecordsWhatWasOnScreenSoAFailureCanBeDiagnosedAfterTheFact()
    {
        var log = new VoiceTypingLog(_logPath);
        var flyout = new VoiceTypingFlyout(log);

        flyout.ForceCloseIfStillOpen();

        // The work runs in the background so the input loop is never blocked by the shell, which
        // means the log is being appended to while this reads it.
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline && !ReadShared(_logPath).Contains("unfiltered survey"))
        {
            Thread.Sleep(100);
        }

        var contents = ReadShared(_logPath);

        Assert.Contains("everything on screen when voice typing was switched off", contents);
        Assert.Contains("unfiltered survey", contents);
        Assert.Contains("every top level window", contents);

        // The snapshot has to actually list windows, or it would prove nothing when the panel lingers.
        var count = int.Parse(contents.Split("switched off (")[1].Split(')')[0]);
        Assert.True(count > 0, "The snapshot listed no windows, so it could not diagnose anything.");
    }

    private static string ReadShared(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        catch (Exception ex) when (ex is IOException or FileNotFoundException or DirectoryNotFoundException)
        {
            return string.Empty;
        }
    }
}
