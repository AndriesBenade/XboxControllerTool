using XboxControllerTool.Application;
using XboxControllerTool.Configuration;
using XboxControllerTool.ConsoleUi;
using XboxControllerTool.ConsoleUi.Screens;
using XboxControllerTool.Core;
using XboxControllerTool.Input;
using XboxControllerTool.Simulation;
using XboxControllerTool.Windows;
using XboxControllerTool.Windows.RawInput;

namespace XboxControllerTool.Tests;

/// <summary>
/// The console window is sized from <see cref="UiDimensions.PreferredRows"/>, so a screen taller
/// than that is clipped at runtime. These tests exist because that limit used to be a hand-typed
/// number that no longer matched the screens.
/// </summary>
public class ScreenHeightTests : IDisposable
{
    private readonly string _settingsPath = Path.Combine(Path.GetTempPath(), $"xct-height-{Guid.NewGuid()}.json");
    private readonly AppSettings _settings = AppSettings.CreateDefault();
    private readonly AppState _appState = new();
    private readonly ControllerSelectionService _selectionService = new();
    private readonly RecordingKeyboard _keyboard = new();

    public void Dispose()
    {
        if (File.Exists(_settingsPath))
        {
            File.Delete(_settingsPath);
        }
    }

    [Fact]
    public void EveryScreenFitsTheConsoleWindowTheAppAsksFor()
    {
        foreach (var (name, screen) in BuildScreens(mappingCount: 0))
        {
            Assert.True(
                screen.BuildLines().Count <= UiDimensions.PreferredRows,
                $"{name} needs {screen.BuildLines().Count} rows but the window is only {UiDimensions.PreferredRows}.");
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(8)]
    public void TheDashboardStaysWithinTheWindowNoMatterHowManyButtonsAreMapped(int mappingCount)
    {
        foreach (var (name, screen) in BuildScreens(mappingCount))
        {
            Assert.True(
                screen.BuildLines().Count <= UiDimensions.PreferredRows,
                $"{name} with {mappingCount} mappings needs {screen.BuildLines().Count} rows.");
        }
    }

    [Fact]
    public void ADashboardWithTooManyMappingsSummarisesTheRestInsteadOfGrowing()
    {
        var text = string.Join('\n', Flatten(BuildScreens(mappingCount: 8).First(s => s.Name == "home").Screen));

        Assert.Contains("more in Settings", text);
    }

    private static IEnumerable<string> Flatten(IScreen screen) =>
        screen.BuildLines().Select(line => string.Concat(line.Segments.Select(segment => segment.Text)));

    private List<(string Name, IScreen Screen)> BuildScreens(int mappingCount)
    {
        var repository = new SettingsRepository(_settingsPath);
        var rawSource = new FakeRawGamepadSource();
        rawSource.Devices.Add(new RawGamepadDevice("045E-02FF", 16));

        var customButtons = new CustomButtonService(_settings, repository, _keyboard, rawSource);
        AddMappings(customButtons, rawSource, mappingCount);

        var uiPreferences = new UiPreferences(_settings, repository, new ConsoleFontController(), new ConsoleWindowController(), new StartupManager());
        var statusScreen = new StatusScreen(_appState, _settings);
        var customButtonsScreen = new CustomButtonsScreen(customButtons);
        var settingsScreen = new SettingsScreen(_settings, repository, uiPreferences, customButtonsScreen);
        var controllerScreen = new ControllerSelectionScreen(_selectionService, _appState);

        var detection = new ButtonDetectionScreen(customButtons);
        detection.OnEnter();

        var confirmedDetection = new ButtonDetectionScreen(customButtons);
        confirmedDetection.OnEnter();
        customButtons.Process(new ButtonTransitions(GamepadButton.Extra, GamepadButton.None), executeActions: false);
        _ = confirmedDetection.BuildLines();

        return
        [
            ("home", new HomeScreen(_appState, customButtons,
            [
                new MenuDestination("SETTINGS", "Speed, dead zones and alerts", settingsScreen),
                new MenuDestination("CONTROLLER", "Choose which pad drives the desktop", controllerScreen),
                new MenuDestination("STATUS", "Full input and configuration detail", statusScreen),
                new MenuDestination("EXIT", "Close XboxControllerTool", null)
            ])),
            ("settings", settingsScreen),
            ("controller", controllerScreen),
            ("status", statusScreen),
            ("custom-buttons", customButtonsScreen),
            ("button-detection", detection),
            ("button-detection-confirmed", confirmedDetection),
            ("button-mapping", new ButtonMappingScreen(customButtons, ButtonIds.ForXInput((ushort)GamepadButton.Extra)))
        ];
    }

    private static void AddMappings(CustomButtonService customButtons, FakeRawGamepadSource rawSource, int count)
    {
        for (var index = 0; index < count; index++)
        {
            var press = new RawGamepadButtonPress("045E-02FF", (ushort)(20 + index), DateTime.UtcNow.AddMilliseconds(-200));
            rawSource.Enqueue(press);
            customButtons.Process(new ButtonTransitions(GamepadButton.None, GamepadButton.None), executeActions: false);
            customButtons.Assign(ButtonIds.ForHid(press.DeviceId, press.Usage), KeyModifiers.Control, "F5");
        }
    }
}
