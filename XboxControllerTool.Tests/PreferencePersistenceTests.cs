using XboxControllerTool.Configuration;
using XboxControllerTool.ConsoleUi;

namespace XboxControllerTool.Tests;

public class PreferencePersistenceTests : IDisposable
{
    private readonly string _directory;
    private readonly string _filePath;

    public PreferencePersistenceTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "XboxControllerToolPrefs_" + Guid.NewGuid());
        Directory.CreateDirectory(_directory);
        _filePath = Path.Combine(_directory, "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void ThemeFontAndGamePauseSurviveARestart()
    {
        var repository = new SettingsRepository(_filePath);
        var settings = AppSettings.CreateDefault();

        settings.ThemeName = "Matrix";
        settings.FontSize = ConsoleFontSize.ExtraLarge;
        settings.PauseOnFocusedGame = false;
        repository.Save(settings);

        var reloaded = new SettingsRepository(_filePath).Load();

        Assert.Equal("Matrix", reloaded.ThemeName);
        Assert.Equal(ConsoleFontSize.ExtraLarge, reloaded.FontSize);
        Assert.False(reloaded.PauseOnFocusedGame);
    }

    [Fact]
    public void SettingsFileFromAnOlderVersionKeepsWorkingAndPicksUpNewDefaults()
    {
        File.WriteAllText(_filePath, """{"Version":1,"MouseSensitivity":21.0,"BoostMultiplier":2.2}""");

        var settings = new SettingsRepository(_filePath).Load();

        Assert.Equal(21.0, settings.MouseSensitivity);
        Assert.Equal("Dark", settings.ThemeName);
        Assert.Equal(ConsoleFontSize.Medium, settings.FontSize);
        Assert.True(settings.PauseOnFocusedGame);
    }

    [Fact]
    public void BlankThemeNameFallsBackToTheDefaultTheme()
    {
        var settings = AppSettings.CreateDefault();
        settings.ThemeName = "   ";

        settings.ClampToValidRanges();

        Assert.Equal("Dark", settings.ThemeName);
    }

    [Theory]
    [InlineData("Dark")]
    [InlineData("Light")]
    [InlineData("Xbox")]
    [InlineData("Girly")]
    [InlineData("Matrix")]
    [InlineData("Ocean")]
    [InlineData("Amber")]
    public void EveryBuiltInThemeResolvesByName(string name)
    {
        var theme = ThemeCatalog.Resolve(name);

        Assert.Equal(name, theme.Name);
    }

    [Fact]
    public void AnUnknownThemeNameResolvesToDarkRatherThanCrashing()
    {
        Assert.Equal("Dark", ThemeCatalog.Resolve("does-not-exist").Name);
        Assert.Equal("Dark", ThemeCatalog.Resolve(null).Name);
    }

    [Fact]
    public void CyclingThemesWrapsInBothDirections()
    {
        var first = ThemeCatalog.All[0];
        var last = ThemeCatalog.All[^1];

        Assert.Equal(last.Name, ThemeCatalog.Next(first, -1).Name);
        Assert.Equal(first.Name, ThemeCatalog.Next(last, 1).Name);
    }

    [Fact]
    public void EveryThemeDefinesADistinctFocusColourFromItsBodyText()
    {
        foreach (var theme in ThemeCatalog.All)
        {
            Assert.NotEqual(theme.Text, theme.Focus);
            Assert.NotEqual(theme.Background, theme.Text);
            Assert.NotEqual(theme.Background, theme.Focus);
        }
    }
}
