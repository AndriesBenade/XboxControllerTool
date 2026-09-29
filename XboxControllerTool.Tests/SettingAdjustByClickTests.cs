using XboxControllerTool.Application;
using XboxControllerTool.Configuration;
using XboxControllerTool.ConsoleUi;
using XboxControllerTool.ConsoleUi.Screens;
using XboxControllerTool.Input;
using XboxControllerTool.Windows;
using XboxControllerTool.Windows.RawInput;

namespace XboxControllerTool.Tests;

/// <summary>
/// A click could select a setting row but never change it: sliders and cycles are driven by left and
/// right, and nothing on screen was a target for those.
/// </summary>
public class SettingAdjustByClickTests : IDisposable
{
    private readonly string _settingsPath = Path.Combine(Path.GetTempPath(), $"xct-adjust-{Guid.NewGuid()}.json");
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
    public void AnAdjustableRowOffersADecreaseAndAnIncreaseTarget()
    {
        var row = MenuItemRow.BuildSetting(
            "Mouse Speed", focused: true, SettingControl.Cycle("X"), 22, ControllerButton.LeftRight, 0);

        Assert.Equal(2, row.Regions.Count);
        Assert.Contains(row.Regions, region => region.Action == MenuAction.Left);
        Assert.Contains(row.Regions, region => region.Action == MenuAction.Right);
    }

    [Fact]
    public void ARowDrivenByAHasNoAdjustTargetsBecauseClickingItAlreadyActivatesIt()
    {
        var row = MenuItemRow.BuildSetting(
            "Custom Buttons", focused: true, SettingControl.Action("Map buttons"), 22, ControllerButton.A, 0);

        Assert.Empty(row.Regions);
    }

    [Fact]
    public void TheTwoTargetsMeetWithoutOverlappingOrLeavingAGap()
    {
        var row = MenuItemRow.BuildSetting(
            "Mouse Speed", focused: true, SettingControl.Cycle("X"), 22, ControllerButton.LeftRight, 0);

        var decrease = row.Regions.Single(region => region.Action == MenuAction.Left);
        var increase = row.Regions.Single(region => region.Action == MenuAction.Right);

        Assert.Equal(decrease.EndColumn + 1, increase.StartColumn);
        Assert.True(decrease.StartColumn < decrease.EndColumn);
        Assert.True(increase.StartColumn < increase.EndColumn);
    }

    [Fact]
    public void TheTargetsSitOverTheControlRatherThanTheLabel()
    {
        const int labelWidth = 22;
        var row = MenuItemRow.BuildSetting(
            "Mouse Speed", focused: true, SettingControl.Cycle("X"), labelWidth, ControllerButton.LeftRight, 0);

        var decrease = row.Regions.Single(region => region.Action == MenuAction.Left);

        // Past the panel border, the focus bar and the whole label.
        Assert.True(decrease.StartColumn >= Panel.RowContentColumn + labelWidth);
    }

    [Fact]
    public void ClickingTheRightHalfRaisesTheSetting()
    {
        var navigator = OpenSettings();
        var before = _settings.MouseSensitivity;
        var (row, region) = FindAdjust("Mouse Speed", MenuAction.Right);

        Assert.True(navigator.TryClickRow(row, region.StartColumn + 1));
        Assert.True(_settings.MouseSensitivity > before, $"Stayed at {before}.");
    }

    [Fact]
    public void ClickingTheLeftHalfLowersTheSetting()
    {
        var navigator = OpenSettings();
        var before = _settings.MouseSensitivity;
        var (row, region) = FindAdjust("Mouse Speed", MenuAction.Left);

        Assert.True(navigator.TryClickRow(row, region.StartColumn + 1));
        Assert.True(_settings.MouseSensitivity < before, $"Stayed at {before}.");
    }

    [Fact]
    public void ClickingOneRowNeverChangesADifferentOne()
    {
        var navigator = OpenSettings();
        var scrollBefore = _settings.ScrollSensitivity;
        var mouseBefore = _settings.MouseSensitivity;

        // Nothing has been highlighted yet, so a naive implementation would adjust the first row.
        var (row, region) = FindAdjust("Scroll Speed", MenuAction.Right);
        Assert.True(navigator.TryClickRow(row, region.StartColumn + 1));

        Assert.True(_settings.ScrollSensitivity > scrollBefore, "The clicked row did not change.");
        Assert.Equal(mouseBefore, _settings.MouseSensitivity);
    }

    [Fact]
    public void AnAdjustedSettingIsSavedStraightAway()
    {
        var navigator = OpenSettings();
        var (row, region) = FindAdjust("Mouse Speed", MenuAction.Right);

        navigator.TryClickRow(row, region.StartColumn + 1);

        var reloaded = new SettingsRepository(_settingsPath).Load();
        Assert.Equal(_settings.MouseSensitivity, reloaded.MouseSensitivity);
    }

    private (int Row, ClickRegion Region) FindAdjust(string label, MenuAction action)
    {
        var lines = BuildSettings().BuildLines();

        for (var row = 0; row < lines.Count; row++)
        {
            var text = string.Concat(lines[row].Segments.Select(segment => segment.Text));

            if (!text.Contains(label, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var region in lines[row].Regions)
            {
                if (region.Action == action)
                {
                    return (row, region);
                }
            }
        }

        throw new InvalidOperationException($"No {action} target found on the '{label}' row.");
    }

    private ScreenNavigator OpenSettings()
    {
        var navigator = new ScreenNavigator(BuildSettings());

        try
        {
            navigator.Render(force: true);
        }
        catch (IOException)
        {
            // No console in the test host; the row map is recorded before anything is drawn.
        }

        return navigator;
    }

    private SettingsScreen BuildSettings()
    {
        var repository = new SettingsRepository(_settingsPath);
        var rawSource = new FakeRawGamepadSource();
        rawSource.Devices.Add(new RawGamepadDevice("045E-02FF", 16));
        var customButtons = new CustomButtonService(_settings, repository, new RecordingKeyboard(), rawSource);

        var uiPreferences = new UiPreferences(
            _settings, repository, new ConsoleFontController(), new ConsoleWindowController(), new StartupManager());

        return new SettingsScreen(_settings, repository, uiPreferences, new CustomButtonsScreen(customButtons));
    }
}
