using XboxControllerTool.Configuration;
using XboxControllerTool.Input;

namespace XboxControllerTool.Tests;

public class SettingsRepositoryTests : IDisposable
{
    private readonly string _directory;
    private readonly string _filePath;

    public SettingsRepositoryTests()
    {
        _directory = Path.Combine(Path.GetTempPath(), "XboxControllerToolTests_" + Guid.NewGuid());
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
    public void Load_MissingFile_CreatesFileWithDefaults()
    {
        var repository = new SettingsRepository(_filePath);

        var settings = repository.Load();

        Assert.True(File.Exists(_filePath));
        Assert.Equal(AppSettings.CreateDefault().MouseSensitivity, settings.MouseSensitivity);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsValues()
    {
        var repository = new SettingsRepository(_filePath);
        var settings = AppSettings.CreateDefault();
        settings.MouseSensitivity = 27.5;
        settings.ControllerMode = ControllerSelectionMode.SpecificController;
        settings.SelectedControllerUserIndex = 2;

        repository.Save(settings);
        var reloaded = repository.Load();

        Assert.Equal(27.5, reloaded.MouseSensitivity);
        Assert.Equal(ControllerSelectionMode.SpecificController, reloaded.ControllerMode);
        Assert.Equal(2, reloaded.SelectedControllerUserIndex);
    }

    [Fact]
    public void Load_CorruptFile_FallsBackToDefaultsAndQuarantinesOriginal()
    {
        File.WriteAllText(_filePath, "{ this is not valid json");
        var repository = new SettingsRepository(_filePath);

        var settings = repository.Load();

        Assert.Equal(AppSettings.CreateDefault().MouseSensitivity, settings.MouseSensitivity);
        Assert.True(Directory.GetFiles(_directory, "*.corrupt-*").Length == 1);
    }

    [Fact]
    public void Load_OutOfRangeValues_AreClampedToValidRanges()
    {
        File.WriteAllText(_filePath, """{"Version":1,"MouseSensitivity":9999,"StickDeadZone":0.99}""");
        var repository = new SettingsRepository(_filePath);

        var settings = repository.Load();

        Assert.True(settings.MouseSensitivity <= 40.0);
        Assert.True(settings.StickDeadZone <= 0.5);
    }

    [Fact]
    public void Load_UnknownVersion_ResetsToDefaults()
    {
        File.WriteAllText(_filePath, """{"Version":99,"MouseSensitivity":5}""");
        var repository = new SettingsRepository(_filePath);

        var settings = repository.Load();

        Assert.Equal(AppSettings.CurrentVersion, settings.Version);
        Assert.Equal(AppSettings.CreateDefault().MouseSensitivity, settings.MouseSensitivity);
    }
}
