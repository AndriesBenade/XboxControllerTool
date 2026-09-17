using XboxControllerTool.Application;
using XboxControllerTool.Configuration;
using XboxControllerTool.Windows;

namespace XboxControllerTool.ConsoleUi.Screens;

public sealed class SettingsScreen : ListMenuScreen
{
    private const int LabelWidth = 22;

    private sealed record SettingRow(
        string Label,
        Func<bool, ConsoleSegment[]> RenderControl,
        Action<int>? Adjust,
        Action? Activate = null,
        IScreen? Target = null);

    private readonly AppSettings _settings;
    private readonly SettingsRepository _repository;
    private readonly UiPreferences _uiPreferences;
    private readonly List<SettingRow> _rows;
    private IReadOnlyList<BrowserChoice> _browsers = BrowserCatalog.Discover();

    public SettingsScreen(AppSettings settings, SettingsRepository repository, UiPreferences uiPreferences, IScreen customButtonsScreen)
    {
        _settings = settings;
        _repository = repository;
        _uiPreferences = uiPreferences;

        _rows =
        [
            new SettingRow("Custom Buttons",
                _ => SettingControl.Action("Map spare controller buttons"),
                null,
                null,
                customButtonsScreen),
            new SettingRow("Theme",
                _ => SettingControl.Cycle(_uiPreferences.CurrentTheme.Name.ToUpperInvariant()),
                _uiPreferences.CycleTheme),
            new SettingRow("Font Size",
                _ => _uiPreferences.FontSizeSupported
                    ? SettingControl.Cycle(FormatFontSize(_settings.FontSize))
                    : [.. SettingControl.Cycle(FormatFontSize(_settings.FontSize)), new ConsoleSegment("  console host only", ConsoleTheme.Label)],
                _uiPreferences.CycleFontSize),
            new SettingRow("Start With Windows",
                _ => SettingControl.Toggle(_uiPreferences.IsStartWithWindowsEnabled()),
                _ => _uiPreferences.ToggleStartWithWindows()),
            new SettingRow("Pause In Games",
                _ => SettingControl.Toggle(_settings.PauseOnFocusedGame),
                _ => _settings.PauseOnFocusedGame = !_settings.PauseOnFocusedGame),
            new SettingRow("Yield To Windows UI",
                _ => SettingControl.Toggle(_settings.YieldToWindowsShell),
                _ => _settings.YieldToWindowsShell = !_settings.YieldToWindowsShell),
            new SettingRow("Browser",
                _ => SettingControl.Cycle(CurrentBrowser().DisplayName.ToUpperInvariant()),
                CycleBrowser),
            new SettingRow("Mouse Speed",
                focused => SettingControl.Gauge(_settings.MouseSensitivity, 1, 40, _settings.MouseSensitivity.ToString("0.0"), focused),
                direction => _settings.MouseSensitivity += direction * 1.0),
            new SettingRow("Scroll Speed",
                focused => SettingControl.Gauge(_settings.ScrollSensitivity, 1, 20, _settings.ScrollSensitivity.ToString("0.0"), focused),
                direction => _settings.ScrollSensitivity += direction * 0.5),
            new SettingRow("Precision Speed",
                focused => SettingControl.Gauge(_settings.PrecisionMultiplier, 0.05, 0.9, $"{_settings.PrecisionMultiplier * 100:0}%", focused),
                direction => _settings.PrecisionMultiplier += direction * 0.05),
            new SettingRow("Precision Scroll",
                focused => SettingControl.Gauge(_settings.ScrollPrecisionMultiplier, 0.05, 0.9, $"{_settings.ScrollPrecisionMultiplier * 100:0}%", focused),
                direction => _settings.ScrollPrecisionMultiplier += direction * 0.05),
            new SettingRow("Stick Dead Zone",
                focused => SettingControl.Gauge(_settings.StickDeadZone, 0.02, 0.5, $"{_settings.StickDeadZone * 100:0}%", focused),
                direction => _settings.StickDeadZone += direction * 0.02),
            new SettingRow("Scroll Dead Zone",
                focused => SettingControl.Gauge(_settings.ScrollDeadZone, 0.02, 0.5, $"{_settings.ScrollDeadZone * 100:0}%", focused),
                direction => _settings.ScrollDeadZone += direction * 0.02),
            new SettingRow("Mouse Acceleration",
                _ => SettingControl.Toggle(_settings.MouseAccelerationEnabled),
                _ => _settings.MouseAccelerationEnabled = !_settings.MouseAccelerationEnabled),
            new SettingRow("Alert Duration",
                focused => SettingControl.Gauge(_settings.NotificationDurationMs, 800, 8000, $"{_settings.NotificationDurationMs / 1000.0:0.0}s", focused),
                direction => _settings.NotificationDurationMs += direction * 250),
            new SettingRow("Alert Position",
                _ => SettingControl.Cycle(SettingsText.Position(_settings.NotificationPosition)),
                direction => _settings.NotificationPosition = CycleEnum(_settings.NotificationPosition, direction)),
            new SettingRow("Audio Feedback",
                _ => SettingControl.Toggle(_settings.AudioFeedbackEnabled),
                _ => _settings.AudioFeedbackEnabled = !_settings.AudioFeedbackEnabled),
            new SettingRow("Restore Defaults",
                _ => SettingControl.Action("Reset all settings"),
                null,
                RestoreDefaults)
        ];
    }

    protected override int ItemCount => _rows.Count;

    public override IReadOnlyList<ConsoleLine> BuildLines()
    {
        var lines = new List<ConsoleLine>(AppShell.Header());

        lines.Add(Panel.Top("SETTINGS"));
        LayoutMetrics.PanelPad(lines);

        for (var i = 0; i < _rows.Count; i++)
        {
            var focused = i == SelectedIndex;
            var row = _rows[i];

            lines.Add(MenuItemRow.BuildSetting(
                row.Label,
                focused,
                row.RenderControl(focused),
                LabelWidth,
                row.Adjust is null ? ControllerButton.A : ControllerButton.LeftRight));
        }

        LayoutMetrics.PanelPad(lines);
        lines.Add(Panel.Bottom());

        lines.AddRange(AppShell.Footer(
            (ControllerButton.LeftRight, "Adjust"),
            (ControllerButton.A, "Activate"),
            (ControllerButton.UpDown, "Navigate"),
            (ControllerButton.B, "Back")));

        return lines;
    }

    protected override NavigationCommand OnConfirm(int index)
    {
        var row = _rows[index];

        if (row.Target is not null)
        {
            return NavigationCommand.Push(row.Target);
        }

        row.Activate?.Invoke();
        return NavigationCommand.None;
    }

    protected override NavigationCommand OnAdjust(int index, int direction)
    {
        _rows[index].Adjust?.Invoke(direction);
        _settings.ClampToValidRanges();
        _repository.Save(_settings);
        return NavigationCommand.None;
    }

    private void RestoreDefaults()
    {
        var defaults = AppSettings.CreateDefault();
        _settings.MouseSensitivity = defaults.MouseSensitivity;
        _settings.ScrollSensitivity = defaults.ScrollSensitivity;
        _settings.PrecisionMultiplier = defaults.PrecisionMultiplier;
        _settings.ScrollPrecisionMultiplier = defaults.ScrollPrecisionMultiplier;
        _settings.StickDeadZone = defaults.StickDeadZone;
        _settings.ScrollDeadZone = defaults.ScrollDeadZone;
        _settings.MouseAccelerationEnabled = defaults.MouseAccelerationEnabled;
        _settings.NotificationDurationMs = defaults.NotificationDurationMs;
        _settings.NotificationPosition = defaults.NotificationPosition;
        _settings.AudioFeedbackEnabled = defaults.AudioFeedbackEnabled;
        _settings.PauseOnFocusedGame = defaults.PauseOnFocusedGame;
        _settings.YieldToWindowsShell = defaults.YieldToWindowsShell;
        _settings.BrowserExecutablePath = defaults.BrowserExecutablePath;
        _repository.Save(_settings);
    }

    private BrowserChoice CurrentBrowser() => BrowserCatalog.Resolve(_browsers, _settings.BrowserExecutablePath);

    private void CycleBrowser(int direction)
    {
        // Browsers can be installed or removed while the app is running, so the list is refreshed
        // whenever the user actually goes looking through it.
        _browsers = BrowserCatalog.Discover();

        var current = CurrentBrowser();
        var index = -1;

        for (var i = 0; i < _browsers.Count; i++)
        {
            if (string.Equals(_browsers[i].ExecutablePath, current.ExecutablePath, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }

        var next = index < 0
            ? (direction > 0 ? 0 : _browsers.Count - 1)
            : (index + direction + _browsers.Count) % _browsers.Count;

        _settings.BrowserExecutablePath = _browsers[next].ExecutablePath;
    }

    private static string FormatFontSize(ConsoleFontSize size) => size switch
    {
        ConsoleFontSize.Small => "SMALL",
        ConsoleFontSize.Large => "LARGE",
        ConsoleFontSize.ExtraLarge => "EXTRA LARGE",
        _ => "MEDIUM"
    };

    private static TEnum CycleEnum<TEnum>(TEnum current, int direction) where TEnum : struct, Enum
    {
        var values = Enum.GetValues<TEnum>();
        var index = Array.IndexOf(values, current);
        var next = (index + direction + values.Length) % values.Length;
        return values[next];
    }
}
