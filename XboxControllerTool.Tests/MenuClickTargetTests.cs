using XboxControllerTool.Application;
using XboxControllerTool.Configuration;
using XboxControllerTool.ConsoleUi;
using XboxControllerTool.ConsoleUi.Screens;
using XboxControllerTool.Input;
using XboxControllerTool.Windows;
using XboxControllerTool.Windows.RawInput;

namespace XboxControllerTool.Tests;

/// <summary>
/// A mouse click arrives as a console row number, so every selectable row has to carry the item it
/// draws. If that mapping is wrong a click silently activates the wrong thing, which is worse than
/// not responding at all.
/// </summary>
public class MenuClickTargetTests : IDisposable
{
    private readonly string _settingsPath = Path.Combine(Path.GetTempPath(), $"xct-click-{Guid.NewGuid()}.json");
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
    public void EveryMenuRowOnTheDashboardIsClickable()
    {
        var indices = ItemRows(BuildHome().BuildLines());

        Assert.Equal([0, 1, 2, 3], indices);
    }

    [Fact]
    public void EverySettingRowIsClickableAndInOrder()
    {
        var screen = BuildSettings();
        var indices = ItemRows(screen.BuildLines());

        Assert.NotEmpty(indices);
        Assert.Equal(Enumerable.Range(0, indices.Count), indices);
    }

    [Fact]
    public void RowsThatAreNotItemsCarryNoTarget()
    {
        var lines = BuildHome().BuildLines();

        // Headings, borders and the hint bar must never be clickable.
        var clickable = lines.Count(line => line.ItemIndex >= 0);

        Assert.Equal(4, clickable);
        Assert.True(lines.Count > clickable, "Every single row claimed to be an item.");
    }

    [Fact]
    public void ClickingARowSelectsThatItemRatherThanTheHighlightedOne()
    {
        var home = BuildHome();
        var navigator = new ScreenNavigator(home);
        var lines = home.BuildLines();
        var thirdItemRow = lines.Select((line, row) => (line, row)).First(entry => entry.line.ItemIndex == 2).row;

        // Rendering is what records the row map, exactly as it happens at runtime.
        RenderInto(navigator);

        Assert.True(navigator.TryClickRow(thirdItemRow));

        // The third destination opens the status screen, so the click must have moved the highlight.
        Assert.IsType<StatusScreen>(navigator.Current);
    }

    [Fact]
    public void ClickingAFrameRowDoesNothing()
    {
        var home = BuildHome();
        var navigator = new ScreenNavigator(home);
        var lines = home.BuildLines();
        var frameRow = lines.Select((line, row) => (line, row)).First(entry => entry.line.ItemIndex < 0).row;

        RenderInto(navigator);

        Assert.False(navigator.TryClickRow(frameRow));
        Assert.Same(home, navigator.Current);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(10_000)]
    public void ClickingOutsideTheRenderedScreenIsIgnored(int row)
    {
        var navigator = new ScreenNavigator(BuildHome());
        RenderInto(navigator);

        Assert.False(navigator.TryClickRow(row));
    }

    [Fact]
    public void TheHighlightCanBeMovedDirectlyToAnyItem()
    {
        var screen = BuildSettings();

        screen.SelectItem(3);
        var lines = screen.BuildLines();

        var focusedRow = lines.Select((line, row) => (line, row))
            .First(entry => Flatten(entry.line).Contains($"{Glyphs.FocusBar}{Glyphs.FocusBar}"));

        Assert.Equal(3, lines[focusedRow.row].ItemIndex);
    }

    [Fact]
    public void AnOutOfRangeSelectionIsClampedRatherThanThrowing()
    {
        var screen = BuildSettings();

        screen.SelectItem(-5);
        Assert.Contains(screen.BuildLines(), line => line.ItemIndex == 0 && Flatten(line).Contains(Glyphs.FocusBar));

        screen.SelectItem(10_000);
        var last = screen.BuildLines().Where(line => line.ItemIndex >= 0).Max(line => line.ItemIndex);
        Assert.Contains(screen.BuildLines(), line => line.ItemIndex == last && Flatten(line).Contains(Glyphs.FocusBar));
    }

    private static void RenderInto(ScreenNavigator navigator)
    {
        try
        {
            navigator.Render(force: true);
        }
        catch (IOException)
        {
            // No console in the test host; the row map is recorded before anything is drawn.
        }
    }

    private static List<int> ItemRows(IReadOnlyList<ConsoleLine> lines) =>
        [.. lines.Where(line => line.ItemIndex >= 0).Select(line => line.ItemIndex)];

    private static string Flatten(ConsoleLine line) => string.Concat(line.Segments.Select(segment => segment.Text));

    private HomeScreen BuildHome()
    {
        var settingsScreen = BuildSettings();
        var statusScreen = new StatusScreen(_appState, _settings);
        var controllerScreen = new ControllerSelectionScreen(_selectionService, _appState);

        return new HomeScreen(_appState, BuildCustomButtons(),
        [
            new MenuDestination("SETTINGS", "Speed, dead zones and alerts", settingsScreen),
            new MenuDestination("CONTROLLER", "Choose which pad drives the desktop", controllerScreen),
            new MenuDestination("STATUS", "Full input and configuration detail", statusScreen),
            new MenuDestination("EXIT", "Close XboxControllerTool", null)
        ]);
    }

    private SettingsScreen BuildSettings()
    {
        var repository = new SettingsRepository(_settingsPath);
        var uiPreferences = new UiPreferences(
            _settings, repository, new ConsoleFontController(), new ConsoleWindowController(), new StartupManager());

        return new SettingsScreen(_settings, repository, uiPreferences, new CustomButtonsScreen(BuildCustomButtons()));
    }

    private CustomButtonService BuildCustomButtons()
    {
        var repository = new SettingsRepository(_settingsPath);
        var rawSource = new FakeRawGamepadSource();
        rawSource.Devices.Add(new RawGamepadDevice("045E-02FF", 16));

        return new CustomButtonService(_settings, repository, new RecordingKeyboard(), rawSource);
    }
}
