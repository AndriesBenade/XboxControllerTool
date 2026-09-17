using XboxControllerTool.Application;
using XboxControllerTool.Configuration;
using XboxControllerTool.ConsoleUi;
using XboxControllerTool.ConsoleUi.Screens;
using XboxControllerTool.Input;

namespace XboxControllerTool.Tests;

public class ConsoleUiRenderingTests : IDisposable
{
    private static readonly char[] AllowedNonAsciiCharacters =
    [
        Glyphs.TopLeft, Glyphs.TopRight, Glyphs.BottomLeft, Glyphs.BottomRight,
        Glyphs.Horizontal, Glyphs.Vertical, Glyphs.TeeLeft, Glyphs.TeeRight,
        Glyphs.BandTopLeft, Glyphs.BandTopRight, Glyphs.BandBottomLeft, Glyphs.BandBottomRight,
        Glyphs.BandHorizontal, Glyphs.BandVertical,
        Glyphs.FocusBar, Glyphs.GaugeFilled, Glyphs.GaugeEmpty
    ];

    private readonly string _settingsPath = Path.Combine(Path.GetTempPath(), $"xct-ui-tests-{Guid.NewGuid()}.json");
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

    public static TheoryData<string> ScreenNames() => new("home", "settings", "controller", "controller-waiting", "status");

    [Theory]
    [MemberData(nameof(ScreenNames))]
    public void EveryPanelLineIsExactlyPanelWidth(string screenName)
    {
        foreach (var line in Render(screenName))
        {
            var text = Flatten(line);

            if (text.Length == 0 || !IsPanelLine(text))
            {
                continue;
            }

            Assert.Equal(Panel.Width, text.Length);
        }
    }

    [Theory]
    [MemberData(nameof(ScreenNames))]
    public void NoScreenUsesCharactersOutsideTheVerifiedConsoleSet(string screenName)
    {
        foreach (var line in Render(screenName))
        {
            foreach (var character in Flatten(line))
            {
                if (character < 128)
                {
                    continue;
                }

                Assert.Contains(character, AllowedNonAsciiCharacters);
            }
        }
    }

    [Theory]
    [MemberData(nameof(ScreenNames))]
    public void NoScreenLineOverflowsTheConsoleWindow(string screenName)
    {
        foreach (var line in Render(screenName))
        {
            Assert.True(Flatten(line).Length <= Panel.Width, $"{screenName} has a line wider than the panel grid.");
        }
    }

    [Fact]
    public void HomeScreenShowsTheControllerMappingAndMenu()
    {
        var text = string.Join('\n', Render("home").Select(Flatten));

        Assert.Contains("CONTROLS", text);
        Assert.Contains("[ A ]", text);
        Assert.Contains("Left Click", text);
        Assert.Contains("[ START ]", text);
        Assert.Contains("[ LT ]", text);
        Assert.Contains("Voice Input", text);
        Assert.Contains("XboxControllerTool", text.Replace(" ", string.Empty));
        Assert.Contains("by Andries Benade", text);
        Assert.Contains("SETTINGS", text);
    }

    [Fact]
    public void EveryScreenEndsWithAContextualHintBar()
    {
        foreach (var screenName in new[] { "home", "settings", "controller", "controller-waiting", "status" })
        {
            var lastLine = Flatten(Render(screenName)[^1]);
            Assert.Contains("[ ", lastLine);
            Assert.Contains(" ]", lastLine);
        }
    }

    [Fact]
    public void FocusedMenuRowIsMarkedWithoutRelyingOnColourAlone()
    {
        var lines = Render("home").Select(Flatten).ToList();
        var focusedRows = lines.Count(line => line.Contains($"{Glyphs.FocusBar}{Glyphs.FocusBar}"));

        Assert.Equal(1, focusedRows);
    }

    private IReadOnlyList<ConsoleLine> Render(string screenName)
    {
        var repository = new SettingsRepository(_settingsPath);
        var statusScreen = new StatusScreen(_appState, _settings);
        var settingsScreen = new SettingsScreen(_settings, repository);
        var controllerScreen = new ControllerSelectionScreen(_selectionService, _appState);

        switch (screenName)
        {
            case "settings":
                return settingsScreen.BuildLines();
            case "controller":
                return controllerScreen.BuildLines();
            case "controller-waiting":
                _selectionService.BeginSelectSpecificController();
                return controllerScreen.BuildLines();
            case "status":
                return statusScreen.BuildLines();
            default:
                var home = new HomeScreen(_appState,
                [
                    new MenuDestination("SETTINGS", "Speed, dead zones and alerts", settingsScreen),
                    new MenuDestination("CONTROLLER", "Choose which pad drives the desktop", controllerScreen),
                    new MenuDestination("STATUS", "Full input and configuration detail", statusScreen),
                    new MenuDestination("EXIT", "Close XboxControllerTool", null)
                ]);
                return home.BuildLines();
        }
    }

    private static string Flatten(ConsoleLine line) => string.Concat(line.Segments.Select(segment => segment.Text));

    private static bool IsPanelLine(string text) =>
        text[0] is Glyphs.TopLeft or Glyphs.BottomLeft or Glyphs.Vertical or Glyphs.TeeLeft
            or Glyphs.BandTopLeft or Glyphs.BandBottomLeft or Glyphs.BandVertical;
}
