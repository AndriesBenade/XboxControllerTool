namespace XboxControllerTool.Windows;

/// <summary>
/// Records what the voice typing panel actually did, because it cannot be observed from a test: it
/// needs a real machine, a microphone and someone speaking. The log is what turns "it still does not
/// close" into a specific window that ignored a specific message.
/// </summary>
public sealed class VoiceTypingLog
{
    private const long MaxBytes = 256 * 1024;

    private readonly string? _path;
    private readonly Lock _gate = new();

    public VoiceTypingLog(string? path)
    {
        _path = path;
    }

    public static VoiceTypingLog CreateDefault()
    {
        try
        {
            var directory = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XboxControllerTool");
            Directory.CreateDirectory(directory);
            return new VoiceTypingLog(System.IO.Path.Combine(directory, "voice-typing.log"));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new VoiceTypingLog(null);
        }
    }

    public string? FilePath => _path;

    public void Begin() => Line($"=== voice typing switched off at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");

    public void Line(string text) => Write($"{DateTime.Now:HH:mm:ss.fff}  {text}");

    public void Snapshot(string title, IReadOnlyList<string> rows)
    {
        Write($"  {title} ({rows.Count}):");

        foreach (var row in rows)
        {
            Write($"    {row}");
        }
    }

    private void Write(string text)
    {
        if (_path is null)
        {
            return;
        }

        lock (_gate)
        {
            try
            {
                TrimIfHuge();
                File.AppendAllText(_path, text + Environment.NewLine);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
            }
        }
    }

    private void TrimIfHuge()
    {
        var file = new FileInfo(_path!);

        if (file.Exists && file.Length > MaxBytes)
        {
            File.Delete(_path!);
        }
    }
}
