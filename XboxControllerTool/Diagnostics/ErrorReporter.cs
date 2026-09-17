namespace XboxControllerTool.Diagnostics;

/// <summary>
/// Surfaces failures instead of letting them either crash the process or pass unnoticed. Errors are
/// written to the console in red regardless of the active theme, so they can never be mistaken for
/// ordinary output, and appended to a log file because the console usually sits minimized.
/// </summary>
public sealed class ErrorReporter
{
    private const ConsoleColor ErrorColour = ConsoleColor.Red;

    private readonly string _logPath;
    private readonly Lock _gate = new();

    public ErrorReporter(string logPath)
    {
        _logPath = logPath;
    }

    public static ErrorReporter CreateDefault()
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "XboxControllerTool");

        try
        {
            Directory.CreateDirectory(directory);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            directory = Path.GetTempPath();
        }

        return new ErrorReporter(Path.Combine(directory, "errors.log"));
    }

    public string LogPath => _logPath;

    public int ErrorCount { get; private set; }

    public Exception? LastError { get; private set; }

    /// <summary>Records a failure the application recovered from and keeps running.</summary>
    public void Report(string context, Exception error)
    {
        lock (_gate)
        {
            ErrorCount++;
            LastError = error;
            AppendToLog(context, error);
            WriteToConsole([
                $"ERROR  {context}",
                $"       {Describe(error)}"
            ]);
        }
    }

    /// <summary>Records a failure the application cannot continue past, and explains where to look.</summary>
    public void ReportFatal(string context, Exception error)
    {
        lock (_gate)
        {
            ErrorCount++;
            LastError = error;
            AppendToLog("FATAL: " + context, error);
            WriteToConsole([
                string.Empty,
                $"XboxControllerTool could not continue: {context}",
                $"  {Describe(error)}",
                $"  Details written to {_logPath}",
                string.Empty
            ]);
        }
    }

    public static string Describe(Exception error) =>
        string.IsNullOrWhiteSpace(error.Message) ? error.GetType().Name : $"{error.GetType().Name}: {error.Message}";

    private static void WriteToConsole(IReadOnlyList<string> messages)
    {
        try
        {
            var originalForeground = Console.ForegroundColor;
            Console.ForegroundColor = ErrorColour;

            foreach (var message in messages)
            {
                Console.WriteLine(message);
            }

            Console.ForegroundColor = originalForeground;
        }
        catch (IOException)
        {
            // A console that has gone away must not turn error reporting into a second failure.
        }
    }

    private void AppendToLog(string context, Exception error)
    {
        try
        {
            File.AppendAllText(
                _logPath,
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  {context}{Environment.NewLine}{error}{Environment.NewLine}{Environment.NewLine}");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
        }
    }
}
