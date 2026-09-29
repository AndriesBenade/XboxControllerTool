using XboxControllerTool.Application;
using XboxControllerTool.Configuration;
using XboxControllerTool.ConsoleUi;
using XboxControllerTool.ConsoleUi.Screens;
using XboxControllerTool.Input;
using XboxControllerTool.Windows;
using XboxControllerTool.Windows.RawInput;

namespace XboxControllerTool.Tests;

/// <summary>
/// Getting out of a screen with a mouse had no route at all: every screen says "[ B ] Back" at the
/// bottom, but that was inert text, and nothing else on a screen goes backwards.
/// </summary>
public class MenuBackNavigationTests : IDisposable
{
    private readonly string _settingsPath = Path.Combine(Path.GetTempPath(), $"xct-back-{Guid.NewGuid()}.json");
    private readonly AppSettings _settings = AppSettings.CreateDefault();
    private readonly AppState _appState = new();
    private readonly ControllerSelectionService _selectionService = new();

    public void Dispose()
    {
        if (File.Exists(_settingsPath))
        {
            File.Delete(_settingsPath);
        }
    }

    [Fact]
    public void TheBackHintIsClickableOnEveryScreenThatOffersIt()
    {
        var hintBar = HintBar.Build(
            (ControllerButton.A, "Select"),
            (ControllerButton.X, "Clear"),
            (ControllerButton.UpDown, "Navigate"),
            (ControllerButton.B, "Back"));

        var back = Assert.Single(hintBar.Regions, region => region.Action == MenuAction.Cancel);

        var text = string.Concat(hintBar.Segments.Select(segment => segment.Text));
        var label = text[back.StartColumn..(back.EndColumn + 1)];

        // The clickable span has to actually cover the words the user sees.
        Assert.Contains("B", label);
        Assert.Contains("Back", label);
    }

    [Fact]
    public void HintsThatDoNothingAreNotClickable()
    {
        var hintBar = HintBar.Build(
            (ControllerButton.UpDown, "Navigate"),
            (ControllerButton.LeftRight, "Change"),
            (ControllerButton.Y, "Hide App"));

        Assert.Empty(hintBar.Regions);
    }

    [Fact]
    public void ClickingTheBackHintLeavesTheScreen()
    {
        var navigator = OpenSettings();
        var lines = navigator.Current.BuildLines();

        var (row, region) = lines
            .Select((line, index) => (index, line))
            .SelectMany(entry => entry.line.Regions.Select(region => (entry.index, region)))
            .First(entry => entry.region.Action == MenuAction.Cancel);

        Assert.True(navigator.TryClickRow(row, region.StartColumn));
        Assert.IsType<HomeScreen>(navigator.Current);
    }

    [Fact]
    public void ClickingBesideTheBackHintDoesNothing()
    {
        var navigator = OpenSettings();
        var lines = navigator.Current.BuildLines();

        var (row, region) = lines
            .Select((line, index) => (index, line))
            .SelectMany(entry => entry.line.Regions.Select(region => (entry.index, region)))
            .First(entry => entry.region.Action == MenuAction.Cancel);

        Assert.False(navigator.TryClickRow(row, region.EndColumn + 2));
        Assert.IsType<SettingsScreen>(navigator.Current);
    }

    [Fact]
    public void EscapeLeavesTheScreen()
    {
        var navigator = OpenSettings();

        navigator.Dispatch(MenuAction.Cancel);

        Assert.IsType<HomeScreen>(navigator.Current);
    }

    [Fact]
    public void BackingOutOfTheDashboardDoesNotQuitTheApp()
    {
        var navigator = new ScreenNavigator(BuildHome());

        navigator.Dispatch(MenuAction.Cancel);

        Assert.IsType<HomeScreen>(navigator.Current);
        Assert.False(navigator.ShouldExit);
    }

    [Fact]
    public void AConsoleInputRecordIsTheSizeWindowsWrites()
    {
        // A char field would marshal as one byte under the default Ansi charset, changing this size
        // and corrupting every record after the first in a batched read.
        Assert.Equal(20, ConsoleInputSource.RecordSize);
    }

    private ScreenNavigator OpenSettings()
    {
        var home = BuildHome();
        var navigator = new ScreenNavigator(home);

        home.SelectItem(0);
        navigator.Dispatch(MenuAction.Confirm);

        Assert.IsType<SettingsScreen>(navigator.Current);
        Render(navigator);
        return navigator;
    }

    /// <summary>Rendering is what records the row map, exactly as it happens at runtime.</summary>
    private static void Render(ScreenNavigator navigator)
    {
        try
        {
            navigator.Render(force: true);
        }
        catch (IOException)
        {
            // No console in the test host; the map is recorded before anything is drawn.
        }
    }

    private HomeScreen BuildHome()
    {
        var repository = new SettingsRepository(_settingsPath);
        var rawSource = new FakeRawGamepadSource();
        rawSource.Devices.Add(new RawGamepadDevice("045E-02FF", 16));
        var customButtons = new CustomButtonService(_settings, repository, new RecordingKeyboard(), rawSource);

        var uiPreferences = new UiPreferences(
            _settings, repository, new ConsoleFontController(), new ConsoleWindowController(), new StartupManager());

        var settingsScreen = new SettingsScreen(_settings, repository, uiPreferences, new CustomButtonsScreen(customButtons));

        return new HomeScreen(_appState, customButtons,
        [
            new MenuDestination("SETTINGS", "Speed, dead zones and alerts", settingsScreen),
            new MenuDestination("CONTROLLER", "Choose which pad drives the desktop", new ControllerSelectionScreen(_selectionService, _appState)),
            new MenuDestination("STATUS", "Full input and configuration detail", new StatusScreen(_appState, _settings)),
            new MenuDestination("EXIT", "Close XboxControllerTool", null)
        ]);
    }
}
