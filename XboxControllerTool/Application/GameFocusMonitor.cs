using XboxControllerTool.Windows;

namespace XboxControllerTool.Application;

public enum GameFocusTransition
{
    None,
    GameFocused,
    GameUnfocused
}

public sealed class GameFocusMonitor
{
    private const int CacheLimit = 64;

    private static readonly TimeSpan NegativeResultLifetime = TimeSpan.FromSeconds(5);

    private readonly IForegroundWindowSource _foregroundSource;
    private readonly IGameClassifier _classifier;
    private readonly Func<DateTime> _clock;
    private readonly Dictionary<int, (GameClassification Result, DateTime EvaluatedUtc)> _cache = [];
    private readonly int _ownProcessId;

    public GameFocusMonitor(IForegroundWindowSource foregroundSource, IGameClassifier classifier, Func<DateTime>? clock = null, int? ownProcessId = null)
    {
        _foregroundSource = foregroundSource;
        _classifier = classifier;
        _clock = clock ?? (() => DateTime.UtcNow);
        _ownProcessId = ownProcessId ?? Environment.ProcessId;
    }

    public bool IsGameFocused { get; private set; }

    public string? FocusedProcessName { get; private set; }

    public string? Reason { get; private set; }

    public GameFocusTransition Update(bool detectionEnabled)
    {
        if (!detectionEnabled)
        {
            return Apply(false, null, null);
        }

        var foreground = _foregroundSource.GetForeground();

        if (foreground.ProcessId == 0 || foreground.ProcessId == _ownProcessId)
        {
            return Apply(false, null, null);
        }

        var classification = ClassifyWithCache(foreground.ProcessId);
        return Apply(classification.IsGame, classification.ProcessName, classification.Reason);
    }

    private GameClassification ClassifyWithCache(int processId)
    {
        var now = _clock();

        if (_cache.TryGetValue(processId, out var cached) &&
            (cached.Result.IsGame || now - cached.EvaluatedUtc < NegativeResultLifetime))
        {
            return cached.Result;
        }

        var classification = _classifier.Classify(processId);

        if (_cache.Count > CacheLimit)
        {
            _cache.Clear();
        }

        _cache[processId] = (classification, now);
        return classification;
    }

    private GameFocusTransition Apply(bool isGameFocused, string? processName, string? reason)
    {
        FocusedProcessName = processName;
        Reason = reason;

        if (isGameFocused == IsGameFocused)
        {
            return GameFocusTransition.None;
        }

        IsGameFocused = isGameFocused;
        return isGameFocused ? GameFocusTransition.GameFocused : GameFocusTransition.GameUnfocused;
    }
}
