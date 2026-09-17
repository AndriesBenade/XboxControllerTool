using XboxControllerTool.Configuration;
using XboxControllerTool.Windows;

namespace XboxControllerTool.Tests;

public class BrowserSelectionTests : IDisposable
{
    private readonly string _fakeBrowser = Path.Combine(Path.GetTempPath(), $"xct-browser-{Guid.NewGuid()}.exe");

    public BrowserSelectionTests() => File.WriteAllText(_fakeBrowser, "not really a browser");

    public void Dispose()
    {
        if (File.Exists(_fakeBrowser))
        {
            File.Delete(_fakeBrowser);
        }
    }

    [Theory]
    [InlineData("\"C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe\"", @"C:\Program Files\Google\Chrome\Application\chrome.exe")]
    [InlineData("\"C:\\Program Files\\Mozilla Firefox\\firefox.exe\" -osint -url \"%1\"", @"C:\Program Files\Mozilla Firefox\firefox.exe")]
    [InlineData(@"C:\Program Files\Internet Explorer\iexplore.exe", @"C:\Program Files\Internet Explorer\iexplore.exe")]
    [InlineData(@"C:\Windows\explorer.exe %1", @"C:\Windows\explorer.exe")]
    [InlineData(@"C:\Program Files\Some Browser\browser.exe -osint -url %1", @"C:\Program Files\Some Browser\browser.exe")]
    [InlineData(@"C:\Tools\exed\runner.exe --flag", @"C:\Tools\exed\runner.exe")]
    public void AnExecutableIsPulledOutOfAShellCommandWhetherOrNotItIsQuoted(string command, string expected)
    {
        Assert.Equal(expected, CommandLine.ExtractExecutablePath(command));
    }

    [Fact]
    public void AnUnquotedPathContainingSpacesIsNotCutShort()
    {
        // How Internet Explorer is actually registered: unquoted, with a space in "Program Files".
        Assert.Equal(
            @"C:\Program Files\Internet Explorer\iexplore.exe",
            CommandLine.ExtractExecutablePath(@"C:\Program Files\Internet Explorer\iexplore.exe"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\"unterminated")]
    public void AMalformedShellCommandYieldsNoExecutable(string command)
    {
        Assert.Null(CommandLine.ExtractExecutablePath(command));
    }

    [Fact]
    public void TheCatalogAlwaysOffersTheSystemDefaultFirst()
    {
        var browsers = BrowserCatalog.Discover();

        Assert.NotEmpty(browsers);
        Assert.True(browsers[0].IsSystemDefault);
        Assert.Equal("System Default", browsers[0].DisplayName);
    }

    [Fact]
    public void EveryDiscoveredBrowserActuallyExistsOnDisk()
    {
        foreach (var browser in BrowserCatalog.Discover().Where(browser => !browser.IsSystemDefault))
        {
            Assert.True(File.Exists(browser.ExecutablePath), $"{browser.DisplayName} points at a missing file.");
            Assert.False(string.IsNullOrWhiteSpace(browser.DisplayName));
        }
    }

    [Fact]
    public void ADiscoveredBrowserIsNeverListedTwice()
    {
        var paths = BrowserCatalog.Discover()
            .Where(browser => !browser.IsSystemDefault)
            .Select(browser => browser.ExecutablePath!)
            .ToList();

        Assert.Equal(paths.Count, paths.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void NoStoredChoiceResolvesToTheSystemDefault()
    {
        Assert.True(BrowserCatalog.Resolve(BrowserCatalog.Discover(), null).IsSystemDefault);
    }

    [Fact]
    public void AStoredChoiceResolvesToItsCatalogEntry()
    {
        var browsers = BrowserCatalog.Discover();

        // Windows always ships Edge, so a machine with no discoverable browser means discovery
        // itself is broken rather than the machine being unusual.
        var installed = Assert.Single(
            browsers,
            browser => browser.ExecutablePath?.EndsWith("msedge.exe", StringComparison.OrdinalIgnoreCase) == true);

        var resolved = BrowserCatalog.Resolve(browsers, installed.ExecutablePath);

        Assert.Equal(installed.DisplayName, resolved.DisplayName);
        Assert.Equal(installed.ExecutablePath, resolved.ExecutablePath);
    }

    [Fact]
    public void AChoiceThatIsNoLongerInstalledIsStillNamedRatherThanSilentlyLost()
    {
        var resolved = BrowserCatalog.Resolve(BrowserCatalog.Discover(), @"C:\Gone\uninstalled-browser.exe");

        Assert.Equal("uninstalled-browser", resolved.DisplayName);
        Assert.False(resolved.IsSystemDefault);
    }

    [Fact]
    public void AChosenBrowserIsUsedInsteadOfTheWindowsDefault()
    {
        var settings = AppSettings.CreateDefault();
        var controller = new DefaultBrowserController(new RecordingKeyboard(), settings);

        settings.BrowserExecutablePath = _fakeBrowser;

        Assert.Equal(_fakeBrowser, controller.ResolveExecutablePath());
    }

    [Fact]
    public void ChangingTheChoiceTakesEffectWithoutRestarting()
    {
        var settings = AppSettings.CreateDefault();
        var controller = new DefaultBrowserController(new RecordingKeyboard(), settings);

        var systemDefault = controller.ResolveExecutablePath();

        settings.BrowserExecutablePath = _fakeBrowser;
        Assert.Equal(_fakeBrowser, controller.ResolveExecutablePath());

        settings.BrowserExecutablePath = null;
        Assert.Equal(systemDefault, controller.ResolveExecutablePath());
    }

    [Fact]
    public void AChosenBrowserThatHasBeenUninstalledFallsBackToTheWindowsDefault()
    {
        var settings = AppSettings.CreateDefault();
        var controller = new DefaultBrowserController(new RecordingKeyboard(), settings);

        var systemDefault = controller.ResolveExecutablePath();
        settings.BrowserExecutablePath = @"C:\Gone\uninstalled-browser.exe";

        Assert.Equal(systemDefault, controller.ResolveExecutablePath());
    }

    [Fact]
    public void TheChosenBrowserSurvivesARestart()
    {
        var path = Path.Combine(Path.GetTempPath(), $"xct-browser-settings-{Guid.NewGuid()}.json");

        try
        {
            var repository = new SettingsRepository(path);
            var settings = AppSettings.CreateDefault();
            settings.BrowserExecutablePath = _fakeBrowser;
            repository.Save(settings);

            Assert.Equal(_fakeBrowser, repository.Load().BrowserExecutablePath);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
