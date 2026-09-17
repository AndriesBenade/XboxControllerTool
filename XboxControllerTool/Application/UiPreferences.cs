using XboxControllerTool.Configuration;
using XboxControllerTool.ConsoleUi;
using XboxControllerTool.Windows;

namespace XboxControllerTool.Application;

public sealed class UiPreferences
{
    private readonly AppSettings _settings;
    private readonly SettingsRepository _repository;
    private readonly ConsoleFontController _fontController;
    private readonly ConsoleWindowController _windowController;
    private readonly StartupManager _startupManager;

    public UiPreferences(
        AppSettings settings,
        SettingsRepository repository,
        ConsoleFontController fontController,
        ConsoleWindowController windowController,
        StartupManager startupManager)
    {
        _settings = settings;
        _repository = repository;
        _fontController = fontController;
        _windowController = windowController;
        _startupManager = startupManager;
    }

    public event Action? SurfaceInvalidated;

    public bool FontSizeSupported => _fontController.IsSupported;

    public Theme CurrentTheme => ThemeCatalog.Resolve(_settings.ThemeName);

    public void ApplySavedPreferences()
    {
        ConsoleTheme.Apply(CurrentTheme);
        ApplyConsoleColors();
        _fontController.TryApply(_settings.FontSize);
        _windowController.ApplyPreferredSize();
    }

    public void CycleTheme(int direction)
    {
        var theme = ThemeCatalog.Next(CurrentTheme, direction);
        _settings.ThemeName = theme.Name;
        _repository.Save(_settings);

        ConsoleTheme.Apply(theme);
        ApplyConsoleColors();
        SurfaceInvalidated?.Invoke();
    }

    public void CycleFontSize(int direction)
    {
        var sizes = Enum.GetValues<ConsoleFontSize>();
        var index = Array.IndexOf(sizes, _settings.FontSize);
        var next = sizes[(index + direction + sizes.Length) % sizes.Length];

        if (!_fontController.TryApply(next))
        {
            return;
        }

        _settings.FontSize = next;
        _repository.Save(_settings);

        _windowController.ApplyPreferredSize();
        _windowController.CenterOnScreen();
        ApplyConsoleColors();
        SurfaceInvalidated?.Invoke();
    }

    public bool IsStartWithWindowsEnabled() => _startupManager.IsEnabled();

    public void ToggleStartWithWindows()
    {
        if (_startupManager.IsEnabled())
        {
            _startupManager.Disable();
        }
        else
        {
            _startupManager.Enable();
        }
    }

    private static void ApplyConsoleColors()
    {
        try
        {
            Console.BackgroundColor = ConsoleTheme.Background;
            Console.ForegroundColor = ConsoleTheme.Text;
            Console.Clear();
        }
        catch (IOException)
        {
        }
    }
}
